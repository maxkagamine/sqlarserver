// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqliteArchive.Helpers;
using SqliteArchive.Nodes;

namespace SqliteArchive;

public class SqlarService : ISqlarService
{
    private readonly SqliteConnection connection;
    private readonly SqlarOptions options;
    private readonly ILogger<SqlarService> logger;

    private readonly DirectoryNode root;

    public SqlarService(SqliteConnection connection, IOptions<SqlarOptions> options, ILogger<SqlarService> logger)
    {
        this.connection = connection;
        this.options = options.Value;
        this.logger = logger;

        root = new DirectoryNode(options.Value.CaseInsensitive ?
            StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        InitializeFileTree();
    }

    public Node? FindPath(string path, bool dereference = false) => FindPath(new Path(path), dereference);

    public Node? FindPath(Path path, bool dereference = false)
    {
        Node node = root;

        foreach (string segment in path)
        {
            // Follow symlinks in the directory path
            if (node is SymbolicLinkNode symlink && TryResolveSymlink(symlink, out Node? target))
            {
                node = target;
            }

            // Can't find the segment in the current node if it's not a directory
            if (node is not DirectoryNode directory)
            {
                logger.LogDebug("Tried to find \"{Path}\" but \"{Ancestor}\" is not a directory.",
                    path.ToString(), node.Path.ToString());

                return null;
            }

            // Find the segment
            var child = directory.FindChild(segment);

            if (child is null)
            {
                return null;
            }

            node = child;
        }

        // Dereference the final segment
        if (dereference && node is SymbolicLinkNode finalSymlink &&
            TryResolveSymlink(finalSymlink, out Node? finalTarget))
        {
            node = finalTarget;
        }

        return node;
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
        try
        {
            var blob = new SqliteBlob(connection, "sqlar", "data", rowId, readOnly: true);
            return size < 0 || blob.Length == size ? blob :
                new ZLibStream(blob, CompressionMode.Decompress, leaveOpen: false);
        }
        catch (SqliteException ex) when (ex.Message.Contains("cannot open view"))
        {
            // TODO: Show a more helpful exception that the table name option needs to be set
            throw;
        }
    }

    /// <summary>
    /// Reads the rows in the sqlar table and builds a virtual file tree.
    /// </summary>
    private void InitializeFileTree()
    {
        using var _ = logger.BeginTimedOperation(nameof(InitializeFileTree));

        using var sql = connection.CreateCommand();
        sql.CommandText = "select rowid, name, mode, mtime, sz, length(data) from sqlar;";
        using var reader = sql.ExecuteReader();

        List<SymbolicLinkNode> symlinks = [];

        while (reader.Read())
        {
            long rowId = reader.GetInt64(0);
            Path path = new(reader.GetString(1));
            Mode mode = reader.GetInt32(2);
            DateTime dateModified = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(3)).UtcDateTime;
            long size = reader.GetInt64(4);
            long compressedSize = reader.IsDBNull(5) ? 0 : reader.GetInt64(5);

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
                logger.LogWarning("Path \"{Path}\" exists in the archive multiple times.", path.ToString());
                continue;
            }

            // Create the node
            node = mode switch
            {
                { IsDirectory: true } => new DirectoryNode(name, mode, dateModified, parent),
                { IsRegularFile: true } => new FileNode(name, mode, dateModified, size, compressedSize, parent, rowId),
                { IsSymbolicLink: true } => new SymbolicLinkNode(name, mode, dateModified, compressedSize, parent, ReadSymlinkTarget(rowId)),
                _ => new Node(name, mode, dateModified, size, compressedSize, parent)
            };

            if (node is SymbolicLinkNode symlink)
            {
                symlinks.Add(symlink);
            }

            parent.AddChild(node);
        }

        // Eagerly resolve symlink targets
        foreach (var symlink in symlinks)
        {
            TryResolveSymlink(symlink, out var _);
        }
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
                directory = new DirectoryNode(segment, Mode.Directory, DateTime.UnixEpoch, parent)
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
                    path.ToString(), node.Path.ToString());

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
    /// <param name="target">The symlink's <see cref="SymbolicLinkNode.TargetNode"/>.</param>
    /// <returns>A boolean indicating whether the symlink target could be resolved.</returns>
    private bool TryResolveSymlink(SymbolicLinkNode symlink, [NotNullWhen(true)] out Node? target)
    {
        if (symlink.ResolutionState == SymlinkResolutionState.Resolved)
        {
            target = symlink.TargetNode;
            return target is not null;
        }

        if (symlink.ResolutionState == SymlinkResolutionState.StartedResolving) // We've looped back
        {
            logger.LogWarning("Recursive symlink detected at \"{Path}\".", symlink.Path.ToString());

            target = null;
            return false;
        }

        Path absoluteTarget = symlink.Parent!.Path + symlink.Target;

        symlink.ResolutionState = SymlinkResolutionState.StartedResolving;
        target = FindPath(absoluteTarget, dereference: true);

        if (target is SymbolicLinkNode)
        {
            // Dereferencing failed either due to broken symlink or recursion
            target = null;
        }

        symlink.TargetNode = target;
        symlink.ResolutionState = SymlinkResolutionState.Resolved;

        return target is not null;
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
