// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqliteArchive.Helpers;

public static class FileSizeFormatter
{
    const double K = 1000;
    const double M = 1000 * K;
    const double G = 1000 * M;
    const double T = 1000 * G;

    const double Ki = 1024;
    const double Mi = 1024 * Ki;
    const double Gi = 1024 * Mi;
    const double Ti = 1024 * Gi;

    /// <summary>
    /// Formats a number of bytes into either binary or SI units. Decimals are rounded off at three significant figures.
    /// </summary>
    /// <param name="bytes">The number of bytes to format.</param>
    /// <param name="si">Whether to use SI units (KB, MB, GB, TB) instead of binary (KiB, MiB, GiB, TiB).</param>
    /// <returns>The formatted string.</returns>
    public static string FormatBytes(long bytes, bool si = false) => ((double)bytes, si) switch
    {
        ( >= 1023.5 * Gi, false) => $"{Round(bytes / Ti)} TiB",
        ( >= 1023.5 * Mi, false) => $"{Round(bytes / Gi)} GiB",
        ( >= 1023.5 * Ki, false) => $"{Round(bytes / Mi)} MiB",
        ( >= Ki, false) => $"{Round(bytes / Ki)} KiB",
        ( >= 999.5 * G, true) => $"{Round(bytes / T)} TB",
        ( >= 999.5 * M, true) => $"{Round(bytes / G)} GB",
        ( >= 999.5 * K, true) => $"{Round(bytes / M)} MB",
        ( >= K, true) => $"{Round(bytes / K)} KB",
        _ => $"{bytes} B"
    };

    private static string Round(double num) => num.ToString(num < 100 ? "G3" : "F0");
}
