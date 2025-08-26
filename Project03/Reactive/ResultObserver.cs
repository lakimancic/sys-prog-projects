using Serilog;

namespace Project03.Reactive;

public class ResultObserver : IObserver<HttpServerResult>
{
    public void OnCompleted()
    {
        Log.Information("ResultObserver: Completed!");
    }

    public void OnError(Exception error)
    {
        Log.Error("ResultObserver Error: {Error}", error.Message);
    }

    void IObserver<HttpServerResult>.OnNext(HttpServerResult value)
    {
        Log.Information("ResultObserver: Result is {Result}", value.RestaurantId);
        HttpServer.Ok(new
        {
            Id = value.RestaurantId
        }, value.Context.Response);
    }
}