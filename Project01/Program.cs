using DotNetEnv;
using Project01.Caches;
using Project01.HttpServer;
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

using var cache = new SpotifyCache(new TimeSpan(1, 30, 0));

HttpServer server = new(cache);

server.Start();

while (Console.ReadKey(intercept: true).Key != ConsoleKey.Escape) ;

server.Stop();
Log.CloseAndFlush();