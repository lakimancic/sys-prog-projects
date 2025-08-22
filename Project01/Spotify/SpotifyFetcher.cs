using Project01.Caches;
using Project01.Models;
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
        public List<T> Items { get; set; } = [];
    }

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

    public List<Track> FetchAllTracks(string query)
    {
        ConcurrentBag<List<Track>> result = [];
        var initial = FetchTracks(query);
        int total = initial.Total;
        result.Add(initial.Items);

        // TODO: Paralelizacija da se prodje sve total, 
        // krece se od sledeci offset ako postoji (50) posto je prvi vec obradjen
        // preporuka da koristis Countdown s ThreadPools

        return [.. result.SelectMany(tracks => tracks)];
    }

    public List<Album> FetchAllAlbums(string query)
    {
        ConcurrentBag<List<Album>> result = [];
        var initial = FetchAlbums(query);
        int total = initial.Total;
        result.Add(initial.Items);

        // TODO: Paralelizacija da se prodje sve total, 
        // krece se od sledeci offset ako postoji (50) posto je prvi vec obradjen
        // preporuka da koristis Countdown s ThreadPools

        return [.. result.SelectMany(albums => albums)];
    }

    private FetchResult<Track> FetchTracks(string query, int offset = 0)
    {
        string url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit=50&offset={offset}";
        var response = httpClient.GetAsync(url).Result;
        response.EnsureSuccessStatusCode();

        string json = response.Content.ReadAsStringAsync().Result;
        using var doc = JsonDocument.Parse(json);
        var tracks = doc.RootElement.GetProperty("tracks");
        return JsonSerializer.Deserialize<FetchResult<Track>>(tracks, opts)!;
    }

    private FetchResult<Album> FetchAlbums(string query, int offset = 0)
    {
    
        string url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=album&limit=50&offset={offset}";
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