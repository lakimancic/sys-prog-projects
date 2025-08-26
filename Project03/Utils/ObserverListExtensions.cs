using System.Net;

namespace Project03.Utils;

public static class ObserverListExtensions
{
    public static void OnNext<T>(this List<IObserver<T>> observers, T value)
    {
        foreach (var observer in observers)
            observer.OnNext(value);
    }

    public static void OnCompleted<T>(this List<IObserver<T>> observers)
    {
        foreach (var observer in observers)
            observer.OnCompleted();
    }

    public static void OnError<T>(this List<IObserver<T>> observers, Exception ex)
    {
        foreach (var observer in observers)
            observer.OnError(ex);
    }
}