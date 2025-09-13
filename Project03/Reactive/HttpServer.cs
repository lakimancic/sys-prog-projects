using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Text.Json;
using Project03.Utils;
using Serilog;

namespace Project03.Reactive;

public class HttpServerResult {
    public required HttpListenerContext Context { get; set; }
    public required string RestaurantId { get; set; }
}

public class HttpServer : IObservable<HttpServerResult>
{
    private readonly List<IObserver<HttpServerResult>> observers = [];
    private readonly HttpListener listener;
    private readonly IScheduler scheduler = TaskPoolScheduler.Default;
    private readonly Thread thread;
    private bool active;
    private static readonly JsonSerializerOptions opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public HttpServer(string address = "localhost", int port = 8080)
    {
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
        Log.Information("HTTP Server: Stopped server.");
    }

    void Listen()
    {
        try
        {
            while (active)
            {
                var context = listener.GetContext();
                scheduler.Schedule(() =>
                {
                    HandleRequest(context);
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

    void HandleRequest(HttpListenerContext context)
    {
        try
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

            var id = request.QueryString.Get("id");


            if (string.IsNullOrEmpty(id))
            {
                BadRequest("Missing id GET parameter", context.Response);
                return;
            }

            observers.OnNext(new HttpServerResult
            {
                Context = context,
                RestaurantId = id
            });
        }
        catch (Exception ex)
        {
            InternalServerError(ex.Message, context.Response);
            observers.OnError(ex);
        }
    }

    public IDisposable Subscribe(IObserver<HttpServerResult> observer)
    {
        if (!observers.Contains(observer))
            observers.Add(observer);

        return Disposable.Create(() => observers.Remove(observer));
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

    public static void InternalServerError(string message, HttpListenerResponse response)
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);

        response.StatusCode = 500;
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.Close();

        Log.Warning("Http Response: Internal Server Error({Message})", message);
    }

    public static void Ok(object obj, HttpListenerResponse response)
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