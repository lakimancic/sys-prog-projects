using System.Collections.Concurrent;

namespace Project01.Caches;

public class GenericCache<T>(TimeSpan ttl)
{
    private ConcurrentDictionary<string, List<T>> cache = [];
    private TimeSpan timeToLive = ttl;
    private Timer? expTimer;

    public void AddToCache(string key, List<T> values)
    {
        cache.AddOrUpdate(key, values, (key, existing) => values);
    }

    
}