using System.Net;
using System.Text.Json;
using Serilog;

namespace Project01.HttpServer;

public class HttpServer(int port)
{
    private readonly HttpListener listener = new();

    public void Start()
    {
        listener.Prefixes.Add($"http://*:{port}/");
        listener.Start();
        Log.Information("Listening on port {Port}... Press Ctrl+C to stop.", port);

        try
        {
            while (true)
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
        catch (HttpListenerException ex)
        {
            Log.Error("HTTP listener error: {Error}", ex.Message);
        }
        finally
        {
            listener.Stop();
        }
    }

    static void HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        string endpoint = context.Request.RemoteEndPoint?.ToString() ?? "unknown";

        Log.Information("Request: {Method} {Url} from {RemoteEndPoint}",
            request.HttpMethod,
            request.Url?.PathAndQuery,
            endpoint);

        var query = request.QueryString.Get("query");
        var type = request.QueryString.Get("type");

        if (query == null)
        {
            BadRequest("Missing query GET parameter", context.Response);
            return;
        }
        if (type == null)
        {
            BadRequest("Missing type GET parameter", context.Response);
            return;
        }

        Ok(new
        {
            Query = query,
            Type = type
        }, context.Response);
    }

    static void BadRequest(string message, HttpListenerResponse response)
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);

        response.StatusCode = 400;
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.Close();
    }

    static void Ok(object obj, HttpListenerResponse response)
    {
        string jsonStr = JsonSerializer.Serialize(obj);
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(jsonStr);

        response.StatusCode = 200;
        response.ContentLength64 = buffer.Length;
        response.ContentType = "application/json";
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.Close();
    }
}