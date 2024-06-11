using Integration.Common;
using Integration.Backend;
using System.Collections.Concurrent;
using StackExchange.Redis;

namespace Integration.Service;

public sealed class ItemIntegrationService
{
    //This is a dependency that is normally fulfilled externally.

    private ItemOperationBackend ItemIntegrationBackend { get; set; } = new();
    private static readonly ConcurrentDictionary<string, Task> InProgressItems = new();
    //private static readonly ConnectionMultiplexer Redis = ConnectionMultiplexer.Connect("localhost");
    private readonly IDatabase RedisDb;
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(30);

    public ItemIntegrationService(IDatabase redisDb)
    {
        RedisDb = redisDb;
    }

    // This is called externally and can be called multithreaded, in parallel.
    // More than one item with the same content should not be saved. However,
    // calling this with different contents at the same time is OK, and should
    // be allowed for performance reasons.
    public async Task<Result> SaveItemAsync(string itemContent)
    {
        // Check the backend to see if the content is already saved.
        if (InProgressItems.ContainsKey(itemContent))
        {
            return new Result(false, $"Duplicate item received with content {itemContent}.");
        }

        string lockKey = $"lock:{itemContent}";
        bool lockAcquired = await RedisDb.LockTakeAsync(lockKey, Environment.MachineName, LockTimeout);

        if (!lockAcquired)
        {
            return new Result(false, $"Duplicate item received with content {itemContent}.");
        }

        var saveTask = new Task<Result>(() =>
        {
            if (ItemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
            {
                return new Result(false, $"Duplicate item received with content {itemContent}.");
            }

            var item = ItemIntegrationBackend.SaveItem(itemContent);
            return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
        });

        if (!InProgressItems.TryAdd(itemContent, saveTask))
        {
            await RedisDb.LockReleaseAsync(lockKey, Environment.MachineName);
            return new Result(false, $"Duplicate item received with content {itemContent}.");
        }

        saveTask.Start();
        var result = await saveTask;
        InProgressItems.TryRemove(itemContent, out _);
        await RedisDb.LockReleaseAsync(lockKey, Environment.MachineName);
        return result;

    }

    public List<Item> GetAllItems()
    {
        return ItemIntegrationBackend.GetAllItems();
    }
}