using Project02.Caches;
using Project02.Models;
using Serilog;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Project02.Spotify;

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
    readonly string clientId = "[REDACTED]";
    readonly string clientSecret = "[REDACTED]";

    public SpotifyFetcher()
    {
        httpClient = new HttpClient();
        accessToken = GetSpotifyToken(clientId, clientSecret);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
    }

    public async Task<List<Track>> FetchAllTracks(string query)
    {
        var initial = await FetchTracks(query);
        int total = initial.Total;

        var allTracks = new List<Track>(initial.Items);

        if (total <= limitSize)
            return allTracks;

        int remaining = total - limitSize;
        int requests = (int)Math.Ceiling((double)remaining / limitSize);

        var tasks = new List<Task<FetchResult<Track>>>();
        for (int i = 1; i <= requests; i++)
        {
            int offset = i * limitSize;
            tasks.Add(FetchTracks(query, offset));
        }
        var results = await Task.WhenAll(tasks);
        foreach (var r in results)
            allTracks.AddRange(r.Items);

        return allTracks;
    }

    public async Task<List<Album>> FetchAllAlbums(string query)
    {
        var initial = await FetchAlbums(query);
        int total = initial.Total;

        var allAlbums = new List<Album>(initial.Items);

        if (total <= 50)
            return allAlbums;

        int remaining = total - 50;
        int requests = (int)Math.Ceiling(remaining / 50.0);

        var tasks = new List<Task<FetchResult<Album>>>();
        for (int i = 1; i <= requests; i++)
        {
            int offset = i * 50;
            tasks.Add(FetchAlbums(query, offset));
        }

        var results = await Task.WhenAll(tasks);
        foreach (var r in results)
            allAlbums.AddRange(r.Items);

        return allAlbums;
    }

    private async Task<FetchResult<Track>> FetchTracks(string query, int offset = 0)
    {
        Log.Information("Fetcher: Fetching tracks with {Query} at offset {Offset}", query, offset);
        string url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit={limitSize}&offset={offset}";
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        // Log.Information("Fetcher: Between fetching tracks with {Query} at offset {Offset}", query, offset);
        string json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var tracks = doc.RootElement.GetProperty("tracks");
        // Log.Information("Fetcher: After fetching tracks with {Query} at offset {Offset}", query, offset);
        return JsonSerializer.Deserialize<FetchResult<Track>>(tracks, opts)!;
    }

    private async Task<FetchResult<Album>> FetchAlbums(string query, int offset = 0)
    {
        Log.Information("Fetcher: Fetching albums with {Query} at offset {Offset}", query, offset);
        string url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=album&limit=50&offset={offset}";
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
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