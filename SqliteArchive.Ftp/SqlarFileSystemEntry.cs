// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using FubarDev.FtpServer.FileSystem;
using FubarDev.FtpServer.FileSystem.Generic;
using SqliteArchive.Nodes;

namespace SqliteArchive.Ftp;

internal class SqlarFileSystemEntry : IUnixFileSystemEntry
{
    public SqlarFileSystemEntry(Node node, string? name = null)
    {
        Node = node;
        Name = name ?? node.Name;
        Permissions = new GenericUnixPermissions(
            user: new GenericAccessMode(
                read: node.Mode.Permissions.HasFlag(SqliteArchive.Permissions.UserRead),
                write: node.Mode.Permissions.HasFlag(SqliteArchive.Permissions.UserWrite),
                execute: node.Mode.Permissions.HasFlag(SqliteArchive.Permissions.UserExecute)),
            group: new GenericAccessMode(
                read: node.Mode.Permissions.HasFlag(SqliteArchive.Permissions.GroupRead),
                write: node.Mode.Permissions.HasFlag(SqliteArchive.Permissions.GroupWrite),
                execute: node.Mode.Permissions.HasFlag(SqliteArchive.Permissions.GroupExecute)),
            other: new GenericAccessMode(
                read: node.Mode.Permissions.HasFlag(SqliteArchive.Permissions.OtherRead),
                write: node.Mode.Permissions.HasFlag(SqliteArchive.Permissions.OtherWrite),
                execute: node.Mode.Permissions.HasFlag(SqliteArchive.Permissions.OtherExecute)));
    }

    internal Node Node { get; }

    public string Name { get; }

    public IUnixPermissions Permissions { get; }

    public DateTimeOffset? LastWriteTime => Node.DateModified;

    public DateTimeOffset? CreatedTime => Node.DateModified;

    public long NumberOfLinks => 1;

    public string Owner => "nobody";

    public string Group => "nogroup";
}
