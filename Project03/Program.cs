
using DotNetEnv;
using Project03.Reactive;
using Serilog;

Env.Load();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss}] {Level:u3} T{ThreadId} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/server-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        buffered: true)
    .CreateLogger();

HttpServer server = new();

var yelpFetch = new YelpFetchCall();
var subscriptionFetcher = server.Subscribe(yelpFetch);

var topicModeler = new TopicModeler();
var subscritionTopics = yelpFetch.Subscribe(topicModeler);

var observer = new ResultObserver();
var subscriptionObserver = topicModeler.Subscribe(observer);

server.Start();

while (Console.ReadKey(intercept: true).Key != ConsoleKey.Escape) ;

server.Stop();
subscriptionObserver.Dispose();
subscritionTopics.Dispose();
subscriptionFetcher.Dispose();

Log.CloseAndFlush();