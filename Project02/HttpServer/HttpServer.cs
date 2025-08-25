using System.Net;
using System.Text.Json;
using Project02.Caches;
using Project02.Spotify;
using Serilog;

namespace Project02.HttpServer;

public class HttpServer
{
    private readonly SpotifyCache cache;
    private readonly HttpListener listener;
    private readonly SpotifyFetcher fetcher;
    private readonly Thread thread;
    private bool active;
    private static readonly JsonSerializerOptions opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public HttpServer(SpotifyCache cache, string address = "localhost", int port = 8080)
    {
        this.cache = cache;
        fetcher = new();
        listener = new();
        listener.Prefixes.Add($"http://{address}:{port}/");
        thread = new(ListenAsync);
        active = false;
    }

    public void Start()
    {
        active = true;
        listener.Start();
        thread.Start();
        Log.Information("HTTP Server: Started listening on {Url}.", listener.Prefixes.First());
    }

    public void Stop()
    {
        active = false;
        thread.Interrupt();
        thread.Join();
        cache.ClearCachedAlbums();
        cache.ClearCachedTracks();
        Log.Information("HTTP Server: Stopped server.");
    }

    async void ListenAsync()
    {
        try
        {
            while (active)
            {
                var context = await listener.GetContextAsync();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await HandleRequestAsync(context);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("HTTP Request error: {Error}", ex.Message);
                    }
                });
            }
        }
        catch (HttpListenerException ex)
        {
            Log.Error("HTTP listener error: {Error}", ex.Message);
        }
        finally
        {
            listener.Stop();
        }
    }

    async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        string endpoint = context.Request.RemoteEndPoint?.ToString() ?? "unknown";

        Log.Information("Request: {Method} {Url} from {RemoteEndPoint}",
            request.HttpMethod,
            request.Url?.PathAndQuery,
            endpoint);

        var query = request.QueryString.Get("query");
        var type = request.QueryString.Get("type");

        if (string.IsNullOrEmpty(query))
        {
            await BadRequest("Missing query GET parameter", context.Response);
            return;
        }
        if (type == null)
        {
            await BadRequest("Missing type GET parameter", context.Response);
            return;
        }
        if (type != "album" && type != "track")
        {
            await BadRequest("Invalid type GET parameter", context.Response);
            return;
        }

        if (type == "album")
        {
            var albums = cache.GetAlbums(query);
            if (albums == null)
            {
                try
                {
                    albums = await fetcher.FetchAllAlbums(query);
                    cache.AddOrUpdateAlbumsCache(query, albums);
                }
                catch (Exception ex)
                {
                    await BadRequest(ex.Message, context.Response);
                    return;
                }
            }
            await Ok(albums, context.Response);
            return;
        }

        if (type == "track")
        {
            var tracks = cache.GetTracks(query);
            if (tracks == null)
            {
                try
                {
                    tracks = await fetcher.FetchAllTracks(query);
                    cache.AddOrUpdateTracksCache(query, tracks);
                }
                catch (Exception ex)
                {
                    await BadRequest(ex.Message, context.Response);
                    return;
                }
            }
            await Ok(tracks, context.Response);
            return;
        }

        await BadRequest("Invalid type GET parameter", context.Response);
    }

    static async Task BadRequest(string message, HttpListenerResponse response)
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);

        response.StatusCode = 400;
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.Close();

        Log.Warning("Http Response: Bad Request({Message})", message);
    }

    static async Task Ok(object obj, HttpListenerResponse response)
    {
        string jsonStr = JsonSerializer.Serialize(obj);
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(jsonStr);

        response.StatusCode = 200;
        response.ContentLength64 = buffer.Length;
        response.ContentType = "application/json";
        await response.OutputStream.WriteAsync(buffer);
        response.Close();

        Log.Information("Http Response: Ok()", jsonStr);
    }
}