// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using System.IO.Compression;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SqliteArchive.Helpers;
using SqliteArchive.Nodes;

namespace SqliteArchive;

public class SqlarService : ISqlarService
{
    private readonly SqliteConnection connection;
    private readonly ILogger<SqlarService> logger;

    private readonly DirectoryNode root = new("", Mode.Directory, default, null);

    public SqlarService(SqliteConnection connection, ILogger<SqlarService> logger)
    {
        this.connection = connection;
        this.logger = logger;

        InitializeFileTree();
    }

    public Node? FindPath(Path path)
    {
        Node node = root;

        foreach (string segment in path)
        {
            if (node is not DirectoryNode directory)
            {
                logger.LogDebug("Tried to find \"{Path}\" but \"{Ancestor}\" is not a directory.",
                    path, node.Path);

                return null;
            }

            var child = directory.FindChild(segment);

            if (child is SymbolicLinkNode symlink)
            {
                child = ResolveSymlink(symlink);
            }

            if (child is null)
            {
                return null;
            }

            node = child;
        }

        return node;
    }

    private void InitializeFileTree()
    {
        using var _ = logger.BeginTimedOperation(nameof(InitializeFileTree));

        using var sql = connection.CreateCommand();
        sql.CommandText = $"select rowid, name, mode, mtime, sz from sqlar;";
        using var reader = sql.ExecuteReader();

        while (reader.Read())
        {
            long rowId = reader.GetInt64(0);
            Path path = new(reader.GetString(1));
            Mode mode = reader.GetInt32(2);
            DateTime dateModified = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(3)).UtcDateTime;
            long size = reader.GetInt64(4);

            // If the sqlite3 cli was invoked with "." it'll contain a row for the root "." itself which we can ignore
            if (path.IsRoot)
            {
                continue;
            }

            // Find the containing directory in the tree, creating any necessary directories along the way
            DirectoryNode? parent = GetOrCreateParentDirectory(path);
            if (parent is null)
            {
                continue;
            }

            // Check if a node already exists
            string name = path.BaseName;
            Node? node = parent.FindChild(name);

            if (node is DirectoryNode { IsImplicit: true } existingDirectory && mode.IsDirectory)
            {
                // Update the directory's metadata
                existingDirectory.Mode = mode;
                existingDirectory.DateModified = dateModified;
                existingDirectory.IsImplicit = false;
                continue;
            }
            
            if (node is not null)
            {
                // Duplicate entries
                logger.LogWarning("Path \"{Path}\" exists in the archive multiple times.", path);
                continue;
            }

            // Create the node
            node = mode switch
            {
                { IsDirectory: true } => new DirectoryNode(name, mode, dateModified, parent),
                { IsRegularFile: true } => new FileNode(name, mode, parent, rowId, dateModified, size),
                { IsSymbolicLink: true } => new SymbolicLinkNode(name, mode, dateModified, parent, ReadSymlinkTarget(rowId)),
                _ => new Node(name, mode, dateModified, size, parent)
            };

            parent.AddChild(node);
        }

        // TODO: Find symlink target nodes now that we have a complete tree
    }

    /// <summary>
    /// Finds <paramref name="path"/>'s parent directory in the tree, creating any necessary directories along the way.
    /// </summary>
    /// <remarks>
    /// Although the sqlite3 CLI creates explicit directory entries, an archive created programmatically may contain
    /// only files, in which case the folder hierarchy is created implicitly a la cloud storage.
    /// </remarks>
    /// <param name="path">The path whose parent directory to return.</param>
    /// <returns>The <see cref="DirectoryNode"/> that should contain <paramref name="path"/>, or <see langword="null"/>
    /// if <paramref name="path"/> is empty or contains an invalid directory path.</returns>
    private DirectoryNode? GetOrCreateParentDirectory(Path path)
    {
        if (path.IsRoot)
        {
            return null;
        }

        DirectoryNode parent = root;

        foreach (string segment in path.Parent)
        {
            var node = parent.FindChild(segment);

            if (node is DirectoryNode directory)
            {
                parent = directory;
            }
            else if (node is null)
            {
                directory = new DirectoryNode(segment, Mode.Directory, default, parent)
                {
                    IsImplicit = true
                };

                parent.AddChild(directory);
                parent = directory;
            }
            else
            {
                // Illogical archive
                logger.LogWarning("Path \"{Path}\" exists in the archive, but \"{Ancestor}\" also exists and is not a directory.",
                    path, node.Path);

                return null;
            }
        }

        return parent;
    }

    /// <summary>
    /// Finds the symlink's target, recursively resolving intermediary symlinks, and updates <see
    /// cref="SymbolicLinkNode.TargetNode"/>.
    /// </summary>
    /// <param name="symlink">The symlink to resolve.</param>
    /// <returns>The symlink's <see cref="SymbolicLinkNode.TargetNode"/>.</returns>
    private Node? ResolveSymlink(SymbolicLinkNode symlink)
    {
        if (symlink.ResolutionState == SymlinkResolutionState.Resolved)
        {
            return symlink.TargetNode;
        }

        if (symlink.ResolutionState == SymlinkResolutionState.StartedResolving) // We've looped back
        {
            logger.LogWarning("Recursive symlink detected at \"{Path}\".", symlink.Path);
            return null;
        }

        Path absoluteTarget = symlink.Parent!.Path + symlink.Target;
        
        symlink.ResolutionState = SymlinkResolutionState.StartedResolving;
        symlink.TargetNode = FindPath(absoluteTarget);
        symlink.ResolutionState = SymlinkResolutionState.Resolved;

        return symlink.TargetNode;
    }

    public Stream GetStream(FileNode file) => GetStream(file.RowId, file.Size);

    /// <summary>
    /// Gets the uncompressed data stream for the given row.
    /// </summary>
    /// <param name="rowId">The row id.</param>
    /// <param name="size">The uncompressed size, or -1 if a symlink.</param>
    private Stream GetStream(long rowId, long size)
    {
        // Return the blob, decompressing if necessary[0]. This approach avoids unnecessary memory allocation compared
        // to a query[1]; however, not using sqlar_uncompress[2] means this will break if another compression algorithm
        // is added in the future. Unfortunately the nuget package doesn't seem to include the sqlar extension, anyway.
        // [0]: https://sqlite.org/sqlar/doc/trunk/README.md
        // [1]: https://github.com/dotnet/efcore/issues/24312
        // [2]: https://www.sqlite.org/sqlar.html#managing_sqlite_archives_from_application_code
        var blob = new SqliteBlob(connection, "sqlar", "data", rowId, readOnly: true);
        return size < 0 || blob.Length == size ? blob :
            new ZLibStream(blob, CompressionMode.Decompress, leaveOpen: false);
    }

    /// <summary>
    /// Reads a symlink's target from its blob stream.
    /// </summary>
    /// <param name="rowId">The symlink row id.</param>
    private string ReadSymlinkTarget(long rowId)
    {
        using var stream = GetStream(rowId, size: -1);
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
