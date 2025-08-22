using Project01.Caches;
using Project01.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Project01.Spotify
{
    public class SpotifyFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessToken;
        private readonly SpotifyCache _cache;
        string clientId = "[REDACTED]";
        string clientSecret = "[REDACTED]";

        public SpotifyFetcher(SpotifyCache cache)
        {
            _httpClient = new HttpClient();
            _accessToken = GetSpotifyToken(clientId, clientSecret);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            _cache = cache;
        }

        public List<Track> FetchTracks(string query)
        {
            var cached = _cache.GetTracks(query);
            if (cached != null)
            {
                Console.WriteLine("Tracks nadjeni u cache:");
                return cached;
            }

            Console.WriteLine("Tracks nisu u cache-u, saljem na spotify...");

            string url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit=20";
            var response = _httpClient.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();

            string json = response.Content.ReadAsStringAsync().Result;

            var doc = JsonDocument.Parse(json);
            var tracks = ParseTracks(json);
            _cache.AddOrUpdateTracksCache(query, tracks);

            return tracks;
        }



        public List<Album> FetchAlbums(string query)
        {
            var cached = _cache.GetAlbums(query);
            if (cached != null)
            {
                Console.WriteLine("Albums nadjeni u cache:");
                return cached;
            }

            Console.WriteLine("Albums nisu u cache-u, saljem na spotify...");

            string url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=album&limit=20";
            var response = _httpClient.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();

            string json = response.Content.ReadAsStringAsync().Result;

            var doc = JsonDocument.Parse(json);
            var albums = ParseAlbums(json);

            _cache.AddOrUpdateAlbumsCache(query, albums);

            return albums;
        }

        private List<Track> ParseTracks(string json)
        {
            var result = new List<Track>();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tracksArray = root.GetProperty("tracks").GetProperty("items");

            foreach (var item in tracksArray.EnumerateArray())
            {
                var track = new Track
                {
                    Id = item.GetProperty("id").GetString()!,
                    Name = item.GetProperty("name").GetString()!,
                    DurationMs = item.GetProperty("duration_ms").GetInt32(),
                    Explicit = item.GetProperty("explicit").GetBoolean(),
                    Href = item.GetProperty("href").GetString()!,
                    Popularity = item.GetProperty("popularity").GetInt32(),
                    PreviewUrl = item.TryGetProperty("preview_url", out var previewProp) && previewProp.ValueKind != JsonValueKind.Null
                        ? previewProp.GetString()!
                        : "",
                    TrackNumber = item.GetProperty("track_number").GetInt32(),
                    Type = item.GetProperty("type").GetString()!,
                    Uri = item.GetProperty("uri").GetString()!,
                    IsPlayable = item.TryGetProperty("is_playable", out var playableProp) && playableProp.ValueKind == JsonValueKind.True,
                    IsLocal = item.TryGetProperty("is_local", out var localProp) && localProp.ValueKind == JsonValueKind.True,

                    Album = new Album
                    {
                        Id = item.GetProperty("album").GetProperty("id").GetString()!,
                        Name = item.GetProperty("album").GetProperty("name").GetString()!,
                        AlbumType = item.GetProperty("album").GetProperty("album_type").GetString()!,
                        Href = item.GetProperty("album").GetProperty("href").GetString()!,
                        ReleaseDate = item.GetProperty("album").GetProperty("release_date").GetString()!,
                        ReleaseDatePrecision = item.GetProperty("album").GetProperty("release_date_precision").GetString()!,
                        Type = item.GetProperty("album").GetProperty("type").GetString()!,
                        Uri = item.GetProperty("album").GetProperty("uri").GetString()!,
                        TotalTracks = item.GetProperty("album").GetProperty("total_tracks").GetInt32(),

                        Artists = item.GetProperty("album").GetProperty("artists")
                            .EnumerateArray()
                            .Select(artist => new Artist
                            {
                                Id = artist.GetProperty("id").GetString()!,
                                Name = artist.GetProperty("name").GetString()!,
                                Href = artist.GetProperty("href").GetString()!,
                                Type = artist.GetProperty("type").GetString()!,
                                Uri = artist.GetProperty("uri").GetString()!
                            })
                            .ToArray()
                    },

                    Artists = item.GetProperty("artists")
                        .EnumerateArray()
                        .Select(artist => new Artist
                        {
                            Id = artist.GetProperty("id").GetString()!,
                            Name = artist.GetProperty("name").GetString()!,
                            Href = artist.GetProperty("href").GetString()!,
                            Type = artist.GetProperty("type").GetString()!,
                            Uri = artist.GetProperty("uri").GetString()!
                        })
                        .ToArray()
                };

                result.Add(track);
            }

            return result;
        }

        private List<Album> ParseAlbums(string json)
        {
            var result = new List<Album>();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var albumsArray = root.GetProperty("albums").GetProperty("items");

            foreach (var item in albumsArray.EnumerateArray())
            {
                var album = new Album
                {
                    Id = item.GetProperty("id").GetString()!,
                    Name = item.GetProperty("name").GetString()!,
                    AlbumType = item.GetProperty("album_type").GetString()!,
                    Href = item.GetProperty("href").GetString()!,
                    ReleaseDate = item.GetProperty("release_date").GetString()!,
                    ReleaseDatePrecision = item.GetProperty("release_date_precision").GetString()!,
                    Type = item.GetProperty("type").GetString()!,
                    Uri = item.GetProperty("uri").GetString()!,
                    TotalTracks = item.GetProperty("total_tracks").GetInt32(),
                    AvailableMarkets = item.GetProperty("available_markets").EnumerateArray()
                        .Select(m => m.GetString() ?? "")
                        .ToArray(),

                    Artists = item.GetProperty("artists")
                        .EnumerateArray()
                        .Select(artist => new Artist
                        {
                            Id = artist.GetProperty("id").GetString()!,
                            Name = artist.GetProperty("name").GetString()!,
                            Href = artist.GetProperty("href").GetString()!,
                            Type = artist.GetProperty("type").GetString()!,
                            Uri = artist.GetProperty("uri").GetString()!
                        })
                        .ToArray()
                };

                result.Add(album);
            }

            return result;
        }
        static string GetSpotifyToken(string clientId, string clientSecret)
        {

            using (var client = new HttpClient())
            {

                string url = "https://accounts.spotify.com/api/token";

                string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
                var postData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });
                var response = client.PostAsync(url, postData).Result;
                response.EnsureSuccessStatusCode();

                string content = response.Content.ReadAsStringAsync().Result;
                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    return doc.RootElement.GetProperty("access_token").GetString();
                }
            }
        }
    }
}
