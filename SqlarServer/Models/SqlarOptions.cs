// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqlarServer.Models;

public record SqlarOptions
{
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
