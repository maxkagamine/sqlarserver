// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using System.Net;

namespace SqliteArchive;

// Note: Most of these options are specific to the presentation layer. Split those out if using this project elsewhere.
public record SqlarOptions
{
    public static readonly string HelpText = """
        Environment variables:

        TZ                 Timezone for displaying date modified (default: UTC)
                           See [3m]8;;https://w.wiki/4Jx\List of tz database time zones]8;;\[m

        LANG               Locale used for formatting (default: en_US)

        SizeFormat         Bytes = Display file sizes in bytes without formatting
                           Binary = Use binary units (KiB, MiB, GiB, TiB) (default)
                           SI = Use SI units (KB, MB, GB, TB)

        DirectoriesFirst   Group directories before files (default: true)

        CaseInsensitive    Treat the archive as a case-insensitive filesystem
                           (default: false)

        StaticSite         Disable directory listing and serve index.html files where
                           present and /404.html when not found (default: false)

        Charset            Sets the charset in the Content-Type header of file streams.
                           Empty string to disable. (default: utf-8)

        EnableFtp          Start the FTP server (default: false)

        FtpPasvRange       Port range used for passive mode. Host and container port
                           range must match. (default: 10000-10099)

        FtpPasvAddress     The FTP server's external IP address (default: 127.0.0.1)
        """;

    public SizeFormat SizeFormat { get; init; }

    public bool DirectoriesFirst { get; init; }

    public bool CaseInsensitive { get; init; }

    public bool StaticSite { get; init; }

    public string Charset { get; init; } = "";

    public bool EnableFtp { get; init; }

    public string FtpPasvRange { get; init; } = "";

    public IPAddress? FtpPasvAddress { get; init; }
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
