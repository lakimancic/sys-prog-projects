using Project02.Caches;
using Project02.HttpServer;
using Serilog;

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

using var cache = new SpotifyCache(new TimeSpan(1, 30, 0));

HttpServer server = new(8080, cache);

await server.Start();