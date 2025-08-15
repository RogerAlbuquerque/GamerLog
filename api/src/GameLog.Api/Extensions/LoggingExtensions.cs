namespace GameLog.Api.Extensions;

public static class LoggingExtensions
{
    /// <summary>Structured one-JSON-object-per-line logging to stdout (container friendly).</summary>
    public static ILoggingBuilder AddGameLogLogging(this ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.UseUtcTimestamp = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        });
        return logging;
    }
}
