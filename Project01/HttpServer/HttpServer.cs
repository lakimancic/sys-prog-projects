using System.Net;
using System.Text.Json;
using Project01.Caches;
using Project01.Spotify;
using Serilog;

namespace Project01.HttpServer;

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
        thread = new(Listen);
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

    void Listen()
    {
        try
        {
            while (active)
            {
                var context = listener.GetContext();
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        HandleRequest((HttpListenerContext)state!);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("HTTP Request error: {Error}", ex.Message);
                    }
                }, context);
            }
        }
        catch (ThreadInterruptedException)
        {
            Log.Information("HTTP listener thread interrupted for shutdown.");
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

    void HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        string endpoint = context.Request.RemoteEndPoint?.ToString() ?? "unknown";

        Log.Information("Request: {Method} {Url} from {RemoteEndPoint}",
            request.HttpMethod,
            request.Url?.PathAndQuery,
            endpoint);

        if (request.HttpMethod != "GET")
        {
            MethodNotAllowed(context.Response);
            return;
        }

        var query = request.QueryString.Get("query");
        var type = request.QueryString.Get("type");

        if (string.IsNullOrEmpty(query))
        {
            BadRequest("Missing query GET parameter", context.Response);
            return;
        }
        if (type == null)
        {
            BadRequest("Missing type GET parameter", context.Response);
            return;
        }

        if (type == "album")
        {
            var albums = cache.GetAlbums(query);
            if (albums == null)
            {
                try
                {
                    albums = fetcher.FetchAllAlbums(query);
                    cache.AddOrUpdateAlbumsCache(query, albums);
                }
                catch (Exception ex)
                {
                    BadRequest(ex.Message, context.Response);
                    return;
                }
            }
            Ok(albums, context.Response);
            return;
        }

        if (type == "track")
        {
            var tracks = cache.GetTracks(query);
            if (tracks == null)
            {
                try
                {
                    tracks = fetcher.FetchAllTracks(query);
                    cache.AddOrUpdateTracksCache(query, tracks);
                }
                catch (Exception ex)
                {
                    BadRequest(ex.Message, context.Response);
                    return;
                }
            }
            Ok(tracks, context.Response);
            return;
        }

        BadRequest("Invalid type GET parameter", context.Response);
    }

    static void MethodNotAllowed(HttpListenerResponse response)
    {
        response.StatusCode = 405;
        response.ContentLength64 = 0;
        response.Close();

        Log.Warning("Http Response: Method Not Allowed()");
    }

    static void BadRequest(string message, HttpListenerResponse response)
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);

        response.StatusCode = 400;
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.Close();

        Log.Warning("Http Response: Bad Request({Message})", message);
    }

    static void Ok(object obj, HttpListenerResponse response)
    {
        string jsonStr = JsonSerializer.Serialize(obj, opts);
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(jsonStr);

        response.StatusCode = 200;
        response.ContentLength64 = buffer.Length;
        response.ContentType = "application/json";
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.Close();

        Log.Information("Http Response: Ok()");
    }
}