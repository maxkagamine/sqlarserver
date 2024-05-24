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

        // While the older LIST command shows the actual Unix permissions, the MLSD command reports "file permissions,
        // whether read, write, execute is allowed for the login id"[0]. The FTP library handles this by comparing the
        // current FTP user (anonymous, in this case) against the file owner and interprets the unix permissions
        // accordingly[1]. So rather than the Unix perms, we should report the FTP capabilities instead: "read only".
        //
        // [0]: https://datatracker.ietf.org/doc/html/rfc3659#section-7
        // [1]: https://github.com/FubarDevelopment/FtpServer/blob/v3.1.2/src/FubarDev.FtpServer.Abstractions/ListFormatters/Facts/PermissionsFact.cs#L105
        var accessMode = new GenericAccessMode(
            read: true,
            write: false,
            execute: node is DirectoryNode // Still show sensible perms in case the client is using LIST instead of MLSD
        );
        Permissions = new GenericUnixPermissions(accessMode, accessMode, accessMode);
    }

    internal Node Node { get; }

    public string Name { get; }

    public IUnixPermissions Permissions { get; }

    public DateTimeOffset? LastWriteTime => Node.DateModified;

    public DateTimeOffset? CreatedTime => Node.DateModified;

    public long NumberOfLinks => 1;

    public string Owner => "anonymous";

    public string Group => "anonymous";
}
