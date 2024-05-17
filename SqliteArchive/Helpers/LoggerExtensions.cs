// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SqliteArchive.Helpers;

public static class LoggerExtensions
{
    // Inspired by SerilogMetrics

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Creating separate start & end message templates")]
    public static IDisposable BeginTimedOperation(this ILogger logger, string message, params string[] args)
    {
        logger.LogInformation(message + ": Starting", args);

        var sw = Stopwatch.StartNew();

        return new TimedOperation(() =>
        {
            logger.LogInformation(message + ": Finished in {Milliseconds:F2} ms", [.. args, sw.Elapsed.TotalMilliseconds]);
        });
    }

    private class TimedOperation(Action onComplete) : IDisposable
    {
        public void Dispose() => onComplete();
    }
}
