// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqlarServer.Models;

public record DirectoryEntry(string Name, string Path, DateTime? DateModified = null, string? FormattedSize = null);
