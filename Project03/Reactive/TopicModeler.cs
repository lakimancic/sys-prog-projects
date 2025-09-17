using System.Net;
using Project03.Models;
using Serilog;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using System.Reactive.Concurrency;
using Project03.Utils;
using System.Text.RegularExpressions;

namespace Project03.Reactive;

public class ReviewData
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
}

public class ReviewTopicResult
{
    public string ReviewId { get; set; } = string.Empty;
    public int TopTopic { get; set; }
    public float[] TopicDistribution { get; set; } = [];
    public List<string> TopWords { get; set; } = [];
}

public class LdaRow
{
    [VectorType]
    public float[] TopicVector { get; set; } = [];
}

public class TopicModelerResult
{
    public required HttpListenerContext Context { get; set; }
    public required List<Review> Reviews { get; set; } = [];
    public List<ReviewTopicResult>? Topics { get; set; } = null;
}

public class TopicModeler : IObserver<YelpResult>, IObservable<TopicModelerResult>
{
    private readonly List<IObserver<TopicModelerResult>> observers = [];
    private readonly IScheduler scheduler = TaskPoolScheduler.Default;
    private readonly MLContext mlContext = new();
    private readonly int numberOfTopics = 5;
    private readonly int topWordsCount = 5;

    public static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Articles & conjunctions
        "a", "an", "the", "and", "or", "but", "so",
        // Pronouns
        "i", "you", "we", "they", "he", "she", "it", "this", "that", "these", "those",
        // Prepositions
        "in", "on", "at", "by", "from", "with", "about", "into", "onto", "over", "under",
        // Auxiliaries / verbs
        "is", "are", "was", "were", "be", "been", "being", "do", "does", "did",
        "have", "has", "had", "can", "could", "should", "would", "will", "shall",
        "may", "might", "must",
        // Determiners / quantifiers
        "my", "your", "our", "their", "his", "her", "its",
        "some", "any", "more", "most", "many", "few", "several", "each", "every",
        // Adverbs / fillers
        "then", "than", "also", "just", "not", "only", "very", "really", "such", "like",
        // Misc
        "there", "here", "because", "get", "got"
    };
    static readonly Regex Tokenizer = new(@"\W+", RegexOptions.Compiled);

    public void OnCompleted()
    {
        Log.Information("TopicModeler Obsersver: Completed!");
    }

    public void OnError(Exception error)
    {
        Log.Error("TopicModeler Error: {Error}", error.Message);
    }

    public void OnNext(YelpResult value)
    {
        scheduler.Schedule(() =>
        {
            try
            {
                var result = GetTopics(value.Reviews);

                observers.OnNext(new TopicModelerResult
                {
                    Context = value.Context,
                    Reviews = value.Reviews,
                    Topics = result
                });
            }
            catch (Exception e)
            {
                HttpServer.InternalServerError(e.Message, value.Context.Response);
            }
        });
    }

    public IDisposable Subscribe(IObserver<TopicModelerResult> observer)
    {
        throw new NotImplementedException();
    }

    public List<ReviewTopicResult> GetTopics(List<Review> reviews)
    {
        var list = reviews
            .Where(r => !string.IsNullOrWhiteSpace(r.Text))
            .Select(r => new ReviewData { Id = r.Id, Text = r.Text.Trim() })
            .ToList();

        if (list.Count == 0) return [];

        var data = mlContext.Data.LoadFromEnumerable(list);

        var pipeline = mlContext.Transforms.Text.FeaturizeText(
            outputColumnName: "Ngrams",
            inputColumnName: nameof(Review.Text))
        .Append(mlContext.Transforms.Text.LatentDirichletAllocation(
            outputColumnName: "TopicVector",
            inputColumnName: "Ngrams",
            numberOfTopics: numberOfTopics,
            maximumNumberOfIterations: 100));

        var model = pipeline.Fit(data);
        var transformed = model.Transform(data);

        var rows = mlContext.Data.CreateEnumerable<LdaRow>(transformed, reuseRowObject: false).ToList();

        var results = new List<ReviewTopicResult>();
        var topicGroups = new Dictionary<int, List<string>>();
        for (int t = 0; t < numberOfTopics; t++)
            topicGroups[t] = [];

        for (int i = 0; i < rows.Count; i++)
        {
            var vec = rows[i].TopicVector ?? [];
            int topTopic = vec.Select((value, index) => new { value, index })
                .OrderByDescending(x => x.value)
                .First().index;

            topicGroups[topTopic].AddRange(Tokenize(list[i].Text));

            results.Add(new ReviewTopicResult
            {
                ReviewId = list[i].Id,
                TopTopic = topTopic,
                TopicDistribution = vec
            });
        }

        foreach (var t in topicGroups.Keys)
        {
            var topWords = topicGroups[t]
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(topWordsCount)
                .Select(g => g.Key)
                .ToList();

            foreach (var r in results.Where(r => r.TopTopic == t))
                r.TopWords = topWords;
        }

        return results;
    }
    
    static string[] Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        return [];

        return [.. Tokenizer
            .Split(text.ToLowerInvariant())
            .Where(w => w.Length > 2 && !Stopwords.Contains(w))];
    }
}