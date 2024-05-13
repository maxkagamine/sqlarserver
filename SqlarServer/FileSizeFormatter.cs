// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqlarServer;

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

    public static string FormatBytes(long bytes, bool si = false) => ((double)bytes, si) switch
    {
        ( >= Ti, false) => $"{bytes / Ti:0.##} TiB",
        ( >= Gi, false) => $"{bytes / Gi:0.##} GiB",
        ( >= Mi, false) => $"{bytes / Mi:0.##} MiB",
        ( >= Ki, false) => $"{bytes / Ki:0} KiB",
        ( >= T, true) => $"{bytes / T:0.##} TB",
        ( >= G, true) => $"{bytes / G:0.##} GB",
        ( >= M, true) => $"{bytes / M:0.##} MB",
        ( >= K, true) => $"{bytes / K:0} KB",
        _ => $"{bytes} B"
    };
}
