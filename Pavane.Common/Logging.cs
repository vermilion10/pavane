using Microsoft.Extensions.Logging;

namespace Pavane.Common;

public static class Logging
{
    private const string Banner = """
            ____
           / __ \____ __   ______ _____  ___
          / /_/ / __ `/ | / / __ `/ __ \/ _ \
         / ____/ /_/ /| |/ / /_/ / / / /  __/
        /_/    \__,_/ |___/\__,_/_/ /_/\___/

        """;

    public static ILoggingBuilder InitTracing(this ILoggingBuilder logging, LogLevel level)
    {
        Console.Error.WriteLine(Banner);
        return logging.ClearProviders()
            .AddSimpleConsole(o => { o.SingleLine = true; o.UseUtcTimestamp = true; o.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ "; })
            .SetMinimumLevel(level)
            .AddFilter("Microsoft", LogLevel.Warning);
    }
}
