using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Text.Json;
using Serilog;

namespace Project03.HttpServer;

class HttpServerResult {
    public required HttpListenerContext Context { get; set; }
    public required string RestaurantId { get; set; }
}

public class HttpServer : IObservable<HttpServerResult>
{
    private readonly List<IObserver<HttpServerResult>> observers = [];
    private readonly IScheduler scheduler = TaskPoolScheduler.Default;
    private static readonly JsonSerializerOptions opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    IDisposable IObservable<HttpServerResult>.Subscribe(IObserver<HttpServerResult> observer)
    {
        if (!observers.Contains(observer))
            observers.Add(observer);

        return Disposable.Create(() => observers.Remove(observer));
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