using Serilog;

namespace Project03.Reactive;

public class ResultObserver : IObserver<TopicModelerResult>
{
    public void OnCompleted()
    {
        Log.Information("ResultObserver: Completed!");
    }

    public void OnError(Exception error)
    {
        Log.Error("ResultObserver Error: {Error}", error.Message);
    }

    void IObserver<TopicModelerResult>.OnNext(TopicModelerResult value)
    {
        Log.Information("ResultObserver: Result is {Result} reviews", value.Reviews.Count);
        HttpServer.Ok(new
        {
            value.Reviews,
            value.Topics
        }, value.Context.Response);
    }
}