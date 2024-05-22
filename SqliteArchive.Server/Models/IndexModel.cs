// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqliteArchive.Server.Models;

public record IndexModel(
    string Path,
    int Count,
    string TotalSize,
    string CompressedSize,
    double Ratio,
    IReadOnlyList<DirectoryEntryModel> Entries);

public record DirectoryEntryModel(
    string Name,
    string Path,
    DateTime? DateModified = null,
    string FormattedSize = "",
    string? Tooltip = null,
    Mode? Mode = null);
