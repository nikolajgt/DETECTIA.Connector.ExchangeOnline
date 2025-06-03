using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks.Dataflow;
using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using Microsoft.CodeAnalysis.Differencing;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Services;

public static class DataflowSyncPipeline
{
    public readonly struct PipelineProcess<TEntity>(TEntity T);

    /// <summary>
    /// Generic fan-out & batch pipeline. Fetches input items, expands them into multiple entities, batches them, and persists them.
    /// </summary>
    [SuppressMessage("ReSharper.DPA", "DPA0007: Large number of DB records", MessageId = "count: 100")]
    public static async Task RunAsync<TInput, TEntity>(
        Func<long, CancellationToken, Task<List<TInput>?>> fetchPageAsync,
        Func<TInput, CancellationToken, Task<IEnumerable<TEntity>>> expandAsync, 
        Func<TEntity[], CancellationToken, Task> persistBatchAsync, 
        Func<TInput, long> keySelector,
        int batchSize = 50,
        int maxDegreeOfParallelism = 5,
        CancellationToken cancellationToken = default)
    {
        var inputBuffer = new BufferBlock<TInput>(new DataflowBlockOptions
        {
            BoundedCapacity = maxDegreeOfParallelism * 2
        });

        var fanOutBlock = new TransformManyBlock<TInput, TEntity>(
            async input => await expandAsync(input, cancellationToken),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                BoundedCapacity = maxDegreeOfParallelism,
                CancellationToken = cancellationToken,
                EnsureOrdered = false
            });

        var batchBlock = new BatchBlock<TEntity>(batchSize);

        var persistBlock = new ActionBlock<TEntity[]>(async batch =>
        {
            await persistBatchAsync(batch, cancellationToken);
        },
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 1,
            BoundedCapacity = maxDegreeOfParallelism,
            CancellationToken = cancellationToken
        });

        // Link blocks
        inputBuffer.LinkTo(fanOutBlock, new DataflowLinkOptions { PropagateCompletion = true });
        fanOutBlock.LinkTo(batchBlock, new DataflowLinkOptions { PropagateCompletion = true });
        batchBlock.LinkTo(persistBlock, new DataflowLinkOptions { PropagateCompletion = true });

        // Seed input
        long lastKey = 0;
        while (true)
        {
            var page = await fetchPageAsync(lastKey, cancellationToken);
            if (page == null || page.Count == 0)
                break;

            foreach (var item in page)
            {
                await inputBuffer.SendAsync(item, cancellationToken);
            }

            lastKey = keySelector(page.Last());
        }

        inputBuffer.Complete();

        // Await completion
        await persistBlock.Completion;
    }
}

