using System.Threading.Tasks.Dataflow;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Services;

public static class DataflowPipeline
{
    /// <summary>
    /// Runs a three-stage dataflow: Fetch → Process → (Group →) Persist.
    /// </summary>
    public static async Task RunAsync<TInput, TProcessed>(
        Func<long, CancellationToken, Task<List<TInput>?>> fetchPageAsync,
        Func<List<TInput>, CancellationToken, Task<List<TProcessed>>> processBatchAsync,
        Func<List<TProcessed>, CancellationToken, Task> persistBatchAsync,
        Func<TInput, long> keySelector,
        int fetchPageSize     = 100,
        int groupSize         = 1,           
        int maxDegreeOfParall = 1,
        CancellationToken cancellationToken = default
    )
    {
        var scanBlock = new TransformBlock<List<TInput>, List<TProcessed>>(async batch
                => await processBatchAsync(batch, cancellationToken),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParall,
                BoundedCapacity        = maxDegreeOfParall / 2,
                CancellationToken      = cancellationToken,
                EnsureOrdered          = false
            });

        ISourceBlock<List<TProcessed>> afterGrouping = scanBlock;
        if (groupSize > 1)
        {
            var batchBlock = new BatchBlock<List<TProcessed>>(groupSize);
            var flatten = new TransformBlock<List<TProcessed>[], List<TProcessed>>(arrays =>
                arrays.SelectMany(inner => inner).ToList(),
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity   = maxDegreeOfParall,
                CancellationToken = cancellationToken
            });
            scanBlock.LinkTo(batchBlock,   new DataflowLinkOptions { PropagateCompletion = true });
            batchBlock.LinkTo(flatten,     new DataflowLinkOptions { PropagateCompletion = true });
            afterGrouping = flatten;
        }

        var persistBlock = new ActionBlock<List<TProcessed>>(async batch =>
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