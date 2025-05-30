﻿using System.Threading.Tasks.Dataflow;
using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Services;



public static class DataflowPipeline
{
    public record PipelineScanProcess<TEntity, TMatch>(IEnumerable<TEntity> Entities, IEnumerable<TMatch> Matches)
        where TMatch : Match
    {

    }
    
    /// <summary>
    /// Runs a three-stage dataflow: Fetch → Process → (Group →) Persist.
    /// </summary>
    public static async Task RunAsync<TInput, TEntity, TMatch>(
        Func<long, CancellationToken, Task<List<TInput>?>> fetchPageAsync,
        Func<List<TInput>, CancellationToken, Task<PipelineScanProcess<TEntity, TMatch>>> processBatchAsync,
        Func<PipelineScanProcess<TEntity, TMatch>, CancellationToken, Task> persistBatchAsync,
        Func<TInput, long> keySelector,
        int groupBatches   = 1,           
        int maxDegreeOfParall = 1,
        CancellationToken cancellationToken = default
    ) where TMatch : Match
    {
        var scanBlock = new TransformBlock<List<TInput>, PipelineScanProcess<TEntity, TMatch>>(async batch
                => await processBatchAsync(batch, cancellationToken),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParall,
                BoundedCapacity        = maxDegreeOfParall / 2,
                CancellationToken      = cancellationToken,
                EnsureOrdered          = false
            });

        ISourceBlock<PipelineScanProcess<TEntity, TMatch>> afterGrouping = scanBlock;
        if (groupBatches > 1)
        {
            var batchBlock = new BatchBlock<PipelineScanProcess<TEntity, TMatch>>(groupBatches);
            var flatten =
                new TransformBlock<PipelineScanProcess<TEntity, TMatch>[], PipelineScanProcess<TEntity, TMatch>>(
                    arrays =>
                    {
                        var allEntities = arrays.SelectMany(x => x.Entities);
                        var allMatches  = arrays.SelectMany(x => x.Matches);
                        return new PipelineScanProcess<TEntity, TMatch>(allEntities, allMatches);
                    },
                    new ExecutionDataflowBlockOptions
                    {
                        BoundedCapacity   = maxDegreeOfParall,
                        CancellationToken = cancellationToken
                    }
                );
            scanBlock.LinkTo(batchBlock,   new DataflowLinkOptions { PropagateCompletion = true });
            batchBlock.LinkTo(flatten,     new DataflowLinkOptions { PropagateCompletion = true });
            afterGrouping = flatten;
        }

        var persistBlock = new ActionBlock<PipelineScanProcess<TEntity, TMatch>>(async batch =>
        {
            await persistBatchAsync(batch, cancellationToken);
        },
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 1,
            BoundedCapacity        = maxDegreeOfParall,
            CancellationToken      = cancellationToken,
            EnsureOrdered          = false
        });

        afterGrouping.LinkTo(persistBlock, new DataflowLinkOptions { PropagateCompletion = true });

        long lastKey = 0;
        while (true)
        {
            var page = await fetchPageAsync(lastKey, cancellationToken);
            if (page == null || page.Count == 0)
                break;

            lastKey = keySelector(page.Last());
            await scanBlock.SendAsync(page, cancellationToken);
        }

        scanBlock.Complete();
        await persistBlock.Completion;
    }
}