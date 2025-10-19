using Project01.Caches;
using Project01.Models;
using Serilog;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Project01.Spotify;

public class SpotifyFetcher
{
    private class FetchResult<T>
    {
        public int Total { get; set; }
        public string? Next { get; set; }
        public List<T> Items { get; set; } = [];
    }

    private const int limitSize = 50;
    private readonly HttpClient httpClient;
    private readonly string accessToken;
    private static readonly JsonSerializerOptions opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    readonly string clientId;
    readonly string clientSecret;

    public SpotifyFetcher()
    {
        httpClient = new HttpClient();
        clientId = Environment.GetEnvironmentVariable("CLIENT_ID") ?? "";
        clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET") ?? "";
        accessToken = GetSpotifyToken(clientId, clientSecret);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
    }

    public List<Track> FetchAllTracks(string query)
    {
        ConcurrentBag<List<Track>> result = [];
        var initial = FetchTracks(query);
        int total = initial.Total;
        result.Add(initial.Items);

        if (total <= limitSize)
            return [.. result.SelectMany(tracks => tracks)];
        int remaining = total - limitSize;
        int requests = (int)Math.Ceiling((double)remaining / limitSize);

        using CountdownEvent countdown = new(requests);

        for (int i = 1; i <= requests; i++)
        {
            int offset = i * limitSize;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var batch = FetchTracks(query, offset);
                    result.Add(batch.Items);
                }
                catch (Exception ex)
                {
                    Log.Error("Fetcher: Something went wrong: {Error}!", ex.Message);
                }
                finally
                {
                    countdown.Signal();
                }
            });
        }

        countdown.Wait();

        return [.. result.SelectMany(tracks => tracks)];
    }

    public List<Album> FetchAllAlbums(string query)
    {
        ConcurrentBag<List<Album>> result = [];
        var initial = FetchAlbums(query);
        int total = initial.Total;
        result.Add(initial.Items);

        if (total <= 50)
            return [.. result.SelectMany(albums => albums)];

        int remaining = total - 50;
        int requests = (int)Math.Ceiling(remaining / 50.0);
        using CountdownEvent countdown = new(requests);

        for (int i = 1; i <= requests; i++)
        {
            int offset = i * 50;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var batch = FetchAlbums(query, offset);
                    result.Add(batch.Items);
                }
                catch (Exception ex)
                {
                    Log.Error("Fetcher: Something went wrong: {Error}!", ex.Message);
                }
                finally
                {
                    countdown.Signal();
                }
            });
        }

        countdown.Wait();

        return [.. result.SelectMany(albums => albums)];
    }

    private FetchResult<Track> FetchTracks(string query, int offset = 0)
    {
        Log.Information("Fetcher: Fetching tracks with {Query} at offset {Offset}", query, offset);
        string url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit={limitSize}&offset={offset}";
        var response = httpClient.GetAsync(url).Result;
        response.EnsureSuccessStatusCode();

        string json = response.Content.ReadAsStringAsync().Result;
        using var doc = JsonDocument.Parse(json);
        var tracks = doc.RootElement.GetProperty("tracks");
        var result = JsonSerializer.Deserialize<FetchResult<Track>>(tracks, opts)!;
        return result;
    }

    private FetchResult<Album> FetchAlbums(string query, int offset = 0)
    {
        Log.Information("Fetcher: Fetching albums with {Query} at offset {Offset}", query, offset);
        string url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=album&limit={limitSize}&offset={offset}";
        var response = httpClient.GetAsync(url).Result;
        response.EnsureSuccessStatusCode();

        string json = response.Content.ReadAsStringAsync().Result;
        using var doc = JsonDocument.Parse(json);
        var albums = doc.RootElement.GetProperty("albums");
        return JsonSerializer.Deserialize<FetchResult<Album>>(albums, opts)!;
    }

    static string GetSpotifyToken(string clientId, string clientSecret)
    {
        using var client = new HttpClient();

        string url = "https://accounts.spotify.com/api/token";

        string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
        var postData = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        ]);
        var response = client.PostAsync(url, postData).Result;
        response.EnsureSuccessStatusCode();

        string content = response.Content.ReadAsStringAsync().Result;
        using JsonDocument doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("access_token").GetString() ?? "";
    }
}