// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqlarServer.Models;

public record IndexModel(string Path, int Count, TimeSpan ExecutionTime, IReadOnlyList<DirectoryEntry> Entries);
