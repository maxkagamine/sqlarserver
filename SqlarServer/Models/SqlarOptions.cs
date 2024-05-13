// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqlarServer.Models;

public class SqlarOptions
{
    public required string ArchivePath { get; set; }

    public required string TableName { get; set; }

    public required string NameColumn { get; set; }

    public required string ModeColumn { get; set; }

    public required string DateModifiedColumn { get; set; }

    public required string SizeColumn { get; set; }

    public required string DataColumn { get; set; }

    public required SizeFormat SizeFormat { get; set; }
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
