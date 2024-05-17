// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqliteArchive.Server;

public record ServerOptions
{
    public static readonly string HelpText = """
        Environment variables:

        TZ                     Timezone for displaying date modified (default: UTC)
                               See [3;34m]8;;https://w.wiki/4Jx\List of tz database time zones]8;;\[m

        TableName              Name of the sqlar table (default: sqlar)

        SizeFormat             Bytes = Display file sizes in bytes without formatting
                               Binary = Use binary units (KiB, MiB, GiB, TiB) (default)
                               SI = Use SI units (KB, MB, GB, TB)

        SortDirectoriesFirst   Group directories before files (default: true)
        """;

    public required string TableName { get; init; }

    public required SizeFormat SizeFormat { get; init; }

    public required bool SortDirectoriesFirst { get; init; }
}

public enum SizeFormat
{
    /// <summary>
    /// Display file sizes in bytes without formatting.
    /// </summary>
    Bytes,

    /// <summary>
    /// Use binary units (KiB, MiB, GiB, TiB).
    /// </summary>
    Binary,

    /// <summary>
    /// Use SI units (KB, MB, GB, TB).
    /// </summary>
    SI
}
