// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqlarServer.Models;

public record SqlarOptions
{
    public required string ArchivePath { get; init; }

    public required string TableName { get; init; }

    public required string NameColumn { get; init; }

    public required string ModeColumn { get; init; }

    public required string DateModifiedColumn { get; init; }

    public required string SizeColumn { get; init; }

    public required string DataColumn { get; init; }

    public required SizeFormat SizeFormat { get; init; }
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
