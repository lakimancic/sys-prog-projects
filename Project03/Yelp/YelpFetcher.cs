using Project03.Models;
using Serilog;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace Project03.Yelp;

public class YelpFetcher
{
    private class FetchResult<T>
    {
        public int Total { get; set; }
        public List<T> Reviews { get; set; } = [];
    }

    private const int limitSize = 50;
    private readonly HttpClient httpClient;
    readonly string accessToken = "[REDACTED]";
    private static readonly JsonSerializerOptions opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public YelpFetcher()
    {
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
    }

    public List<Review> FetchAllReviews(string id)
    {
        Log.Information("Fetcher: Fetching reviews for business {BusinessId}", id);

        ConcurrentBag<List<Review>> result = [];
        var initial = FetchReviews(id, 0);
        int total = initial.Total;
        result.Add(initial.Reviews);

        if (total <= limitSize)
            return [.. result.SelectMany(r => r)];
        int remaining = total - limitSize;
        int requests = (int)Math.Ceiling(remaining / (double)limitSize);
        using CountdownEvent countdown = new(requests);

        for (int i = 1; i <= requests; i++)
        {
            int offset = i * limitSize;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var batch = FetchReviews(id, offset);
                    result.Add(batch.Reviews);
                }
                catch (Exception ex)
                {
                    Log.Error("YelpFetcher: Something went wrong: {Error}!", ex.Message);
                }
                finally
                {
                    countdown.Signal();
                }
            });
        }

        countdown.Wait();

        return [.. result.SelectMany(r => r)];
    }

    private FetchResult<Review> FetchReviews(string id, int offset = 0)
    {
        Log.Information("YelpFetcher: Fetching reviews for {BusinessId} at offset {Offset}", id, offset);

        var url = $"https://api.yelp.com/v3/businesses/{id}/reviews?limit={limitSize}&offset={offset}";
        var response = httpClient.GetAsync(url).Result;
        response.EnsureSuccessStatusCode();

        string json = response.Content.ReadAsStringAsync().Result;
        using var doc = JsonDocument.Parse(json);
        var reviews = doc.RootElement.GetProperty("reviews");
        var result = JsonSerializer.Deserialize<FetchResult<Review>>(reviews, opts)!;
        return result;
    }
}