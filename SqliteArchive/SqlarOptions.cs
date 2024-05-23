// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqliteArchive;

public record SqlarOptions
{
    public static readonly string HelpText = """
        Environment variables:

        TZ                     Timezone for displaying date modified (default: UTC)
                               See [3m]8;;https://w.wiki/4Jx\List of tz database time zones]8;;\[m

        LANG                   Locale used for formatting (default: en_US)

        SizeFormat             Bytes = Display file sizes in bytes without formatting
                               Binary = Use binary units (KiB, MiB, GiB, TiB) (default)
                               SI = Use SI units (KB, MB, GB, TB)

        SortDirectoriesFirst   Group directories before files (default: true)
        
        CaseInsensitive        Treat the archive as a case-insensitive filesystem
                               (default: false)

        Charset                Sets the charset in the Content-Type header of file
                               streams. Empty string to disable. (default: utf-8)
        """;

    public SizeFormat SizeFormat { get; init; }

    public bool SortDirectoriesFirst { get; init; }

    public bool CaseInsensitive { get; init; }

    public string Charset { get; init; } = "";
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
