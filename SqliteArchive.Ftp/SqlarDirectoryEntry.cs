// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using FubarDev.FtpServer.FileSystem;
using SqliteArchive.Nodes;

namespace SqliteArchive.Ftp;

internal class SqlarDirectoryEntry : SqlarFileSystemEntry, IUnixDirectoryEntry
{
    public SqlarDirectoryEntry(DirectoryNode node, string? name = null) : base(node, name)
    { }

    public bool IsRoot => Node.Path.IsRoot;

    public bool IsDeletable => false;
}
