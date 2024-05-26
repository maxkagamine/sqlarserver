// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using FubarDev.FtpServer.FileSystem;
using SqliteArchive.Nodes;

namespace SqliteArchive.Ftp;

internal class SqlarFileEntry : SqlarFileSystemEntry, IUnixFileEntry
{
    public SqlarFileEntry(Node node, string? name = null) : base(node, name)
    { }

    public long Size => Node.Size;
}
