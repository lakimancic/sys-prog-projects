
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