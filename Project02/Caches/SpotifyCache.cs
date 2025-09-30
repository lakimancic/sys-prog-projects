using System.Collections.Concurrent;
using Project02.Models;
using Serilog;

namespace Project02.Caches;

public enum CacheStatus
{
    Inserted,
    Updated,
    Unchanged
}

public class SpotifyCache : IDisposable
{
    private readonly ConcurrentDictionary<string, List<Track>> cachedTracks = [];
    private readonly ConcurrentDictionary<string, List<Album>> cachedAlbums = [];
    private readonly TimeSpan timeToLive;
    private readonly Timer? expTimer;

    public SpotifyCache(TimeSpan ttl)
    {
        timeToLive = ttl;
        expTimer = new Timer(_ => RemoveExpired(), null, 0, (int)TimeSpan.FromMinutes(10).TotalMilliseconds);
    }

    public CacheStatus AddOrUpdateTracksCache(string key, List<Track> values)
    {
        return cachedTracks.AddOrUpdate(key,
            inkey =>
            {
                Log.Information("Cache: {Query} added to cached tracks.", key);
                return values;
            },
            (inkey, existing) =>
            {
                if (existing.SequenceEqual(values))
                {
                    Log.Information("Cache: No changes in cached tracks.");
                    return existing;
                }
                else
                {
                    Log.Information("Cache: {Query} cached tracks entry updated.", key);
                    return values;
                }
            }
        ) == values ? CacheStatus.Inserted : CacheStatus.Updated;
    }

    public CacheStatus AddOrUpdateAlbumsCache(string key, List<Album> values)
    {
        return cachedAlbums.AddOrUpdate(key,
            inkey =>
            {
                Log.Information("Cache: {Query} added to cached albums.", key);
                return values;
            },
            (inkey, existing) =>
            {
                if (existing.SequenceEqual(values))
                {
                    Log.Information("Cache: No changes in cached albums.");
                    return existing;
                }
                else
                {
                    Log.Information("Cache: {Query} cached albums entry updated.", key);
                    return values;
                }
            }
        ) == values ? CacheStatus.Inserted : CacheStatus.Updated;
    }

    public void ClearCachedTracks()
    {
        cachedTracks.Clear();
        Log.Information("Cache: All cached tracks entries cleared.");
    }

    public void ClearCachedAlbums()
    {
        cachedAlbums.Clear();
        Log.Information("Cache: All cached albums entries cleared.");
    }

    public bool RemoveTrack(string key)
    {
        if (cachedTracks.TryRemove(key, out _))
        {
            Log.Information("Cache: {Query} cached tracks entry removed.", key);
            return true;
        }
        return false;
    }

    public bool RemoveAlbum(string key)
    {
        if (cachedAlbums.TryRemove(key, out _))
        {
            Log.Information("Cache: {Query} cached albums entry removed.", key);
            return true;
        }
        return false;
    }

    public void RemoveExpired()
    {
        DateTime expTime = DateTime.Now.Subtract(timeToLive);
        foreach (var key in cachedTracks.Keys.ToList())
        {
            if (cachedTracks.TryGetValue(key, out var value) && value is List<Track> values)
            {
                lock (values)
                {
                    int originalCount = values.Count;
                    values.RemoveAll(val => val.CreatedAt < expTime);
                    int removedCount = originalCount - values.Count;

                    if (removedCount > 0)
                    {
                        Log.Information("Cache: Removed {Count} expired items from cashed tracks.", removedCount);
                    }

                    if (values.Count == 0)
                    {
                        if (cachedTracks.TryRemove(key, out _))
                        {
                            Log.Information("Cache: Removed {Query} expired cached tracks entry.", key);
                        }
                    }
                }
            }
        }

        foreach (var key in cachedAlbums.Keys.ToList())
        {
            if (cachedAlbums.TryGetValue(key, out var value) && value is List<Album> values)
            {
                lock (values)
                {
                    int originalCount = values.Count;
                    values.RemoveAll(val => val.CreatedAt < expTime);
                    int removedCount = originalCount - values.Count;

                    if (removedCount > 0)
                    {
                        Log.Information("Cache: Removed {Count} expired items from cashed albums.", removedCount);
                    }

                    if (values.Count == 0)
                    {
                        if (cachedTracks.TryRemove(key, out _))
                        {
                            Log.Information("Cache: Removed {Query} expired cached albums entry.", key);
                        }
                    }
                }
            }
        }
    }

    public List<Track>? GetTracks(string key)
    {
        try
        {
            if (cachedTracks.TryGetValue(key, out var values))
            {
                Log.Information("Cache: Read tracks from cashed tracks for key {Query}", key);
                return values;
            }
            else
            {
                return null;
            }
        }
        catch (Exception e)
        {
            Log.Error("Cache: Error accessing cached tracks entry {Query}: {Error}", key, e.Message);
            return null;
        }
    }

    public List<Album>? GetAlbums(string key)
    {
        try
        {
            if (cachedAlbums.TryGetValue(key, out var values))
            {
                Log.Information("Cache: Read albums from cashed albums for key {Query}", key);
                return values;
            }
            else
            {
                return null;
            }
        }
        catch (Exception e)
        {
            Log.Error("Cache: Error accessing cached albums entry {Query}: {Error}", key, e.Message);
            return null;
        }
    }

    public void Dispose()
    {
        expTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}