// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using FubarDev.FtpServer.BackgroundTransfer;
using FubarDev.FtpServer.FileSystem;
using Microsoft.Extensions.Options;
using SqliteArchive.Nodes;
using System.Diagnostics.CodeAnalysis;

namespace SqliteArchive.Ftp;

internal class SqlarFileSystem : IUnixFileSystem
{
    private readonly ISqlarService sqlarService;

    public SqlarFileSystem(ISqlarService sqlarService, IOptions<SqlarOptions> options)
    {
        this.sqlarService = sqlarService;

        FileSystemEntryComparer = options.Value.CaseInsensitive ?
            StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        Root = new SqlarDirectoryEntry((DirectoryNode)sqlarService.FindPath("/")!);
    }

    public StringComparer FileSystemEntryComparer { get; }

    public IUnixDirectoryEntry Root { get; }

    public Task<IReadOnlyList<IUnixFileSystemEntry>> GetEntriesAsync(IUnixDirectoryEntry directoryEntry, CancellationToken cancellationToken)
    {
        var dir = (DirectoryNode)((SqlarDirectoryEntry)directoryEntry).Node;
        var list = dir.Children.Select(n => CreateEntry(n)).ToList();

        return Task.FromResult<IReadOnlyList<IUnixFileSystemEntry>>(list);
    }

    public Task<IUnixFileSystemEntry?> GetEntryByNameAsync(IUnixDirectoryEntry directoryEntry, string name, CancellationToken cancellationToken)
    {
        var dir = (DirectoryNode)((SqlarDirectoryEntry)directoryEntry).Node;
        var node = dir.Children.FirstOrDefault(n => FileSystemEntryComparer.Equals(n.Name, name));
        var entry = CreateEntry(node);

        return Task.FromResult(entry);
    }

    public Task<Stream> OpenReadAsync(IUnixFileEntry fileEntry, long startPosition, CancellationToken cancellationToken)
    {
        Node node = ((SqlarFileEntry)fileEntry).Node;

        if (node is SymbolicLinkNode symlink)
        {
            if (symlink.IsBroken)
            {
                throw new Exception($"\"{node.Path}\" is a broken symlink.");
            }

            node = symlink.TargetNode;
        }

        if (node is not FileNode file)
        {
            throw new Exception($"\"{node.Path}\" is not a regular file.");
        }

        var stream = sqlarService.GetStream(file);

        if (startPosition > 0)
        {
            if (stream.CanSeek) // SqliteBlob
            {
                stream.Seek(startPosition, SeekOrigin.Begin);
            }
            else // ZLibStream
            {
                const int BufferSize = 1024;

                long readBytes = 0;
                Span<byte> buffer = stackalloc byte[BufferSize];
                while (readBytes < startPosition)
                {
                    int bytesToRead = (int)Math.Min(startPosition - readBytes, BufferSize);
                    stream.ReadExactly(buffer[..bytesToRead]);
                    readBytes += bytesToRead;
                }
            }
        }

        return Task.FromResult(stream);
    }

    [return: NotNullIfNotNull(nameof(node))]
    private static IUnixFileSystemEntry? CreateEntry(Node? node, string? name = null) => node switch
    {
        SymbolicLinkNode { IsBroken: false } symlink => CreateEntry(symlink.TargetNode, node.Name),
        DirectoryNode dir => new SqlarDirectoryEntry(dir, name),
        not null => new SqlarFileEntry(node, name),
        null => null
    };

    #region Write operations
    public bool SupportsAppend => false;

    public bool SupportsNonEmptyDirectoryDelete => false;

    public Task<IBackgroundTransfer?> AppendAsync(IUnixFileEntry fileEntry, long? startPosition, Stream data, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<IBackgroundTransfer?> CreateAsync(IUnixDirectoryEntry targetDirectory, string fileName, Stream data, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<IUnixDirectoryEntry> CreateDirectoryAsync(IUnixDirectoryEntry targetDirectory, string directoryName, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<IUnixFileSystemEntry> MoveAsync(IUnixDirectoryEntry parent, IUnixFileSystemEntry source, IUnixDirectoryEntry target, string fileName, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<IBackgroundTransfer?> ReplaceAsync(IUnixFileEntry fileEntry, Stream data, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<IUnixFileSystemEntry> SetMacTimeAsync(IUnixFileSystemEntry entry, DateTimeOffset? modify, DateTimeOffset? access, DateTimeOffset? create, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task UnlinkAsync(IUnixFileSystemEntry entry, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }
    #endregion
}
