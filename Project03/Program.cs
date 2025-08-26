
using Project03.Reactive;
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

HttpServer server = new();

ResultObserver observer = new();
var subscription = server.Subscribe(observer);

server.Start();

while (Console.ReadKey(intercept: true).Key != ConsoleKey.Escape) ;

server.Stop();
subscription.Dispose();

Log.CloseAndFlush();