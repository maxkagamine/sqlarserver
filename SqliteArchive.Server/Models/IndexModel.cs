// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqliteArchive.Server.Models;

public record IndexModel(string Path, int Count, IReadOnlyList<DirectoryEntryModel> Entries);

public record DirectoryEntryModel(
    string Name,
    string Path,
    DateTime? DateModified = null,
    string FormattedSize = "",
    Mode? Mode = null);
