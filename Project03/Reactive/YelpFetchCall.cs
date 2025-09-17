using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Project03.Models;
using Project03.Utils;
using Project03.Yelp;
using Serilog;

namespace Project03.Reactive;

public class YelpResult {
    public required HttpListenerContext Context { get; set; }
    public required List<Review> Reviews { get; set; } = [];
}

public class YelpFetchCall : IObserver<HttpServerResult>, IObservable<YelpResult>
{
    private readonly List<IObserver<YelpResult>> observers = [];
    private readonly IScheduler scheduler = TaskPoolScheduler.Default;
    private readonly YelpFetcher yelpFetcher = new();
    
    public void OnCompleted()
    {
        Log.Information("YelpFetchCall Obsersver: Completed!");
    }

    public void OnError(Exception error)
    {
        Log.Error("YelpFetchCall Error: {Error}", error.Message);
    }

    public void OnNext(HttpServerResult value)
    {
        scheduler.Schedule(() =>
        {
            try
            {
                var result = yelpFetcher.FetchAllReviews(value.RestaurantId);

                observers.OnNext(new YelpResult
                {
                    Context = value.Context,
                    Reviews = result
                });
            }
            catch (Exception e)
            {
                HttpServer.InternalServerError(e.Message, value.Context.Response);
            }
        });
    }

    public IDisposable Subscribe(IObserver<YelpResult> observer)
    {
        if (!observers.Contains(observer))
            observers.Add(observer);

        return Disposable.Create(() => observers.Remove(observer));
    }
}