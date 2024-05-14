using CacheTower;
using CacheTower.Serializers;
using DiscordEqueBot.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DiscordEqueBot.Utility.Cache;

public class DatabaseCacheLayer : IDistributedCacheLayer
{
    public DatabaseCacheLayer(DatabaseContext databaseContext)
    {
        DatabaseContext = databaseContext;
    }

    private DatabaseContext DatabaseContext { get; }

    public async ValueTask FlushAsync()
    {
        await DatabaseContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"KVCache\"");
    }

    public async ValueTask CleanupAsync()
    {
        // do nothing
    }

    public async ValueTask EvictAsync(string cacheKey)
    {
        await DatabaseContext.KVCache.Where(kv => kv.Key == cacheKey).ExecuteDeleteAsync();
    }

    public async ValueTask<CacheEntry<T>?> GetAsync<T>(string cacheKey)
    {
        var kvCache = DatabaseContext.KVCache.FirstOrDefault(kv => kv.Key == cacheKey);
        if (kvCache == null)
        {
            return null;
        }

        try
        {
            kvCache.hits++;
            await DatabaseContext.SaveChangesAsync();
            var value = JsonConvert.DeserializeObject<T>(kvCache.Value);
            return new CacheEntry<T>(value, kvCache.ExpiredAt ?? DateTime.MinValue);
        }
        catch (Exception ex)
        {
            throw new CacheSerializationException(
                "A serialization error has occurred when deserializing with Newtonsoft.Json", ex);
        }
    }


    public async ValueTask SetAsync<T>(string cacheKey, CacheEntry<T> cacheEntry)
    {
        var kvCache = await DatabaseContext.KVCache.FirstOrDefaultAsync(kv => kv.Key == cacheKey);

        if (kvCache != null)
        {
            kvCache.Value = JsonConvert.SerializeObject(cacheEntry.Value);
            kvCache.UpdatedAt = DateTime.Now;
            kvCache.ExpiredAt = cacheEntry.Expiry;
        }
        else
        {
            kvCache = new KVCache
            {
                Key = cacheKey,
                Value = JsonConvert.SerializeObject(cacheEntry.Value),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                ExpiredAt = cacheEntry.Expiry
            };
        }

        await DatabaseContext.SaveChangesAsync();
    }

    public async ValueTask<bool> IsAvailableAsync(string cacheKey)
    {
        var kvCache = await DatabaseContext.KVCache.FirstOrDefaultAsync(kv => kv.Key == cacheKey);
        return kvCache != null;
    }
}
