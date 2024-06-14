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
                           [3mSee ]8;;https://w.wiki/4Jx\List of tz database time zones]8;;\[m

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

        BlobTable          Name of the table holding the blob (default: sqlar)
                           [3mSee the tip in ]8;;https://github.com/maxkagamine/sqlarserver#readme\the readme]8;;\ on using a view for the sqlar table[m
        
        BlobColumn         Name of the column holding the blob (default: data)
                           [3mSee the tip in ]8;;https://github.com/maxkagamine/sqlarserver#readme\the readme]8;;\ on using a view for the sqlar table[m
        
        EnableFtp          Start the FTP server (default: false)

        FtpPasvPorts       Port range used for passive mode. Host and container ports
                           must match. Avoid too large a range, as many ports can make
                           docker slow. (default: 10000-10009)

        FtpPasvAddress     The FTP server's external IP address (default: 127.0.0.1)
        """;

    public SizeFormat SizeFormat { get; init; }

    public bool DirectoriesFirst { get; init; }

    public bool CaseInsensitive { get; init; }

    public bool StaticSite { get; init; }

    public string Charset { get; init; } = "";

    public string BlobTable { get; init; } = "";

    public string BlobColumn { get; init; } = "";

    public bool EnableFtp { get; init; }

    public string FtpPasvPorts { get; init; } = "";

    public string FtpPasvAddress { get; init; } = "";

    public (int MinPort, int MaxPort, IPAddress Address) ParseFtpPasvOptions()
    {
        int dash = FtpPasvPorts.IndexOf('-');

        if (!int.TryParse(dash == -1 ? FtpPasvPorts : FtpPasvPorts[0..dash], out int minPort) ||
            !int.TryParse(dash == -1 ? FtpPasvPorts : FtpPasvPorts[(dash + 1)..], out int maxPort))
        {
            throw new FormatException($"Could not parse {nameof(FtpPasvPorts)}.");
        }

        if (minPort < 1024)
        {
            throw new ArgumentOutOfRangeException($"{nameof(FtpPasvPorts)} must be at least 1024.");
        }

        if (maxPort < minPort)
        {
            throw new ArgumentOutOfRangeException($"{nameof(FtpPasvPorts)} max port is smaller than the min port.");
        }

        if (!IPAddress.TryParse(FtpPasvAddress, out var address))
        {
            throw new FormatException($"Could not parse {nameof(FtpPasvAddress)}.");
        }

        return (minPort, maxPort, address);
    }
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
