// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqlarServer.Models;

record IndexModel(string Path, IReadOnlyList<ItemModel> Items);

record ItemModel(string Name, string Path, DateTime? DateModified = null, string? FormattedSize = null);
