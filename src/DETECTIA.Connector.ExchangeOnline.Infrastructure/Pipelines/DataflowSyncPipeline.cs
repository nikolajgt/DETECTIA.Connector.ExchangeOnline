using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks.Dataflow;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Pipelines;

public static class DataflowSyncPipeline
{

    /// <summary>
    /// Generic fan-out & batch pipeline. Fetches input items, expands them into multiple entities, batches them, and persists them.
    /// </summary>
    public static async Task RunAsync<TInput, TEntity>(
        Func<long, CancellationToken, Task<List<TInput>?>> fetchPageAsync,
        Func<TInput, CancellationToken, Task<IEnumerable<TEntity>>> expandAsync, 
        Func<TEntity[], CancellationToken, Task> persistBatchAsync, 
        Func<TInput, long> keySelector,
        int persistBatchSize = 500,
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

        var batchBlock = new BatchBlock<TEntity>(persistBatchSize);

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

        await persistBlock.Completion;
    }
    
    /// <summary>
    /// Generic fan-out & batch pipeline. Fetches input items, expands them into multiple entities, flatten the entities, batches them, and persists them.
    /// </summary>
     public static async Task RunAsync<TInput, TEntity1, TEntity2>(
        Func<long, CancellationToken, Task<List<TInput>?>> fetchPageAsync,
        Func<TInput, CancellationToken, Task<(IEnumerable<TEntity1> folders, TEntity2 user)>> expandAsync,
        Func<IEnumerable<TEntity1>, IEnumerable<TEntity2>, CancellationToken, Task> persistBatchAsync,
        Func<TInput, long> keySelector,
        int persistBatchSize = 500,
        int maxDegreeOfParallelism = 5,
        CancellationToken cancellationToken = default)
    {
        var inputBuffer = new BufferBlock<TInput>(new DataflowBlockOptions
        {
            BoundedCapacity = maxDegreeOfParallelism * 2
        });

        var fanOutBlock = new TransformBlock<TInput, (IEnumerable<TEntity1> folders, TEntity2 user)>(
            async input => await expandAsync(input, cancellationToken),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                BoundedCapacity = maxDegreeOfParallelism,
                CancellationToken = cancellationToken,
                EnsureOrdered = false
            });

        var batchBlock = new BatchBlock<(IEnumerable<TEntity1> folders, TEntity2 user)>(persistBatchSize);

        var flattenBlock = new TransformBlock<(IEnumerable<TEntity1> folders, TEntity2 user)[], (IEnumerable<TEntity1>, IEnumerable<TEntity2>)>(
            batch =>
            {
                var allFolders = batch.SelectMany(item => item.folders).ToList();
                var allUsers = batch.Select(item => item.user).ToList();
                return (allFolders, allUsers);
            });

        var persistBlock = new ActionBlock<(IEnumerable<TEntity1>, IEnumerable<TEntity2>)>(
            async tuple =>
            {
                var (folders, users) = tuple;
                await persistBatchAsync(folders, users, cancellationToken);
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
        batchBlock.LinkTo(flattenBlock, new DataflowLinkOptions { PropagateCompletion = true });
        flattenBlock.LinkTo(persistBlock, new DataflowLinkOptions { PropagateCompletion = true });

        // Feed input
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
        await persistBlock.Completion;
    }
}

