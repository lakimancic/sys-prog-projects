using System.Net;
using System.Text.Json;
using Project02.Caches;
using Project02.Spotify;
using Serilog;

namespace Project02.HttpServer;

public class HttpServer(int port, SpotifyCache cache)
{
    private readonly HttpListener listener = new();
    private readonly SpotifyFetcher fetcher = new();
    private static readonly JsonSerializerOptions opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task Start()
    {
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        Log.Information("Listening on port {Port}... Press Ctrl+C to stop.", port);

        try
        {
            while (true)
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

        Log.Information("Http Response: Ok({ResponseData})", jsonStr);
    }
}