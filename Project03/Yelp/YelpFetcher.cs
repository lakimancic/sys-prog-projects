using System.Text.Json;
using Project03.Models;

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
    private static readonly JsonSerializerOptions opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public YelpFetcher()
    {
        httpClient = new HttpClient();
    }

    public List<Review> FetchAllReviews(string id)
    {
        // TODO: Fetch svi reviews kao u Project01
        return [];
    }

    private FetchResult<Review> FetchReviews(string id, int offset = 0)
    {
        // TODO: Fetch reviews s offset kao u Project01
        return new FetchResult<Review>
        {
            Total = 0
        };
    }
}