// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SqliteArchive.Helpers;
using SqliteArchive.Nodes;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace SqliteArchive;

// TODO: Move back to web project as "IndexItemModel"
public record DirectoryEntry(string Name, string Path, DateTime? DateModified = null, string? FormattedSize = null);

public class SqlarService : ISqlarService
{
    // https://github.com/torvalds/linux/blob/master/include/uapi/linux/stat.h
    private const int S_IFMT = 0xF000;
    private const int S_IFDIR = 0x4000;
    private const int S_IFREG = 0x8000;

    private readonly SqliteConnection connection;
    private readonly ILogger<SqlarService> logger;

    private readonly DirectoryNode root = new("", Mode.Directory);

    public SqlarService(SqliteConnection connection, ILogger<SqlarService> logger)
    {
        this.connection = connection;
        this.logger = logger;

        InitializeFileTree();
    }

    private void InitializeFileTree()
    {
        using var sql = connection.CreateCommand();
        sql.CommandText = $"select rowid, name, mode, mtime, sz from sqlar;";
        using var reader = sql.ExecuteReader();

        while (reader.Read())
        {
            long rowId = reader.GetInt64(0);
            string[] path = SplitPath(reader.GetString(1));
            Mode mode = reader.GetInt32(2);
            DateTime dateModified = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(3)).UtcDateTime;
            long size = reader.GetInt64(4);

            // Find the containing directory in the tree, creating any necessary directories along the way
            DirectoryNode? parent = GetOrCreateParentDirectory(path);
            if (parent is null)
            {
                continue;
            }

            // Check if a node already exists
            string name = path[^1];
            Node? node = parent.FindChild(name);

            if (node is DirectoryNode existingDirectory && mode.IsDirectory)
            {
                // Update the directory's metadata, as it may have been created implicitly
                //
                // TODO: Should we display modified date & size for directories? Could display total size instead of "4
                // KiB". Symlinks also have an mtime separate from their targets (and a size, which is just the length
                // of the target string), though maybe we should dereference symlinks like `ls -L`. Should add a check
                // here if the directory was actually created implicitly, or log a warning if duplicate.
                existingDirectory.Mode = mode;
                continue;
            }
            
            if (node is not null)
            {
                // Duplicate entries
                logger.LogWarning("Path \"{Path}\" exists in the archive multiple times.", JoinPath(path));
                continue;
            }

            // Create the node
            node = mode switch
            {
                { IsDirectory: true } => new DirectoryNode(name, mode, parent),
                { IsRegularFile: true } => new FileNode(name, mode, parent, rowId, dateModified, size),
                { IsSymbolicLink: true } => new SymbolicLinkNode(name, mode, parent, ReadSymlinkTarget(rowId)),
                _ => new Node(name, mode, parent)
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
    private DirectoryNode? GetOrCreateParentDirectory(string[] path)
    {
        if (path.Length == 0)
        {
            // Archive contains a directory entry for the root itself, which we'll ignore
            return null;
        }

        DirectoryNode parent = root;

        foreach (string segment in path[..^1])
        {
            var node = parent.FindChild(segment);

            if (node is DirectoryNode directory)
            {
                parent = directory;
            }
            else if (node is null)
            {
                directory = new DirectoryNode(segment, Mode.Directory, parent);

                parent.AddChild(directory);
                parent = directory;
            }
            else
            {
                // Illogical archive
                logger.LogWarning("Path \"{Path}\" exists in the archive, but \"{Ancestor}\" also exists and is not a directory.",
                    JoinPath(path), node.Path);

                return null;
            }
        }

        return parent;
    }

    private static string[] SplitPath(string path)
    {
        if (path is "" or ".")
        {
            return [];
        }

        if (path.StartsWith("./"))
        {
            path = path[2..];
        }

        return path.Split('/', StringSplitOptions.RemoveEmptyEntries);
    }

    private static string JoinPath(string[] path)
        => $"/{string.Join('/', path)}";

    public IEnumerable<DirectoryEntry>? ListDirectory(string path)
    {
        path = NormalizePath(path, isDirectory: true);
        using var _ = logger.BeginTimedOperation($"{nameof(ListDirectory)} {{Path}}", path);

        // Search the db for an explicit directory entry matching the given path, if present (as the directory may exist
        // but be empty), and any and all descendents (not only direct children, as its children may include implicit
        // directories; e.g. "foo" implicitly contains a directory "bar" if a row "foo/bar/blah/stuff" exists)
        using var sql = connection.CreateCommand();

        if (path == "/")
        {
            sql.CommandText = $"select name, mode, mtime, sz from sqlar;";
        }
        else
        {
            sql.CommandText = $"""
                select name, mode, mtime, sz from sqlar
                where
                    ((name =         $trimmedPath or
                      name =  '/' || $trimmedPath or
                      name = './' || $trimmedPath) and
                     (mode & {S_IFMT}) = {S_IFDIR}) or
                    name like         $trimmedPathEscaped || '/%' escape '\' or
                    name like  '/' || $trimmedPathEscaped || '/%' escape '\' or
                    name like './' || $trimmedPathEscaped || '/%' escape '\';
                """;

            sql.Parameters.AddWithValue("$trimmedPath", path.Trim('/'));
            sql.Parameters.AddWithValue("$trimmedPathEscaped", path.Trim('/').Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_"));
        }

        using var reader = sql.ExecuteReader();

        // Return null if no results, unless listing the archive root, in which case the archive is simply empty
        if (!reader.HasRows)
        {
            return path == "/" ? Array.Empty<DirectoryEntry>() : null;
        }

        // Build list of direct children
        Dictionary<string, DirectoryEntry> entries = [];

        while (reader.Read())
        {
            bool isDirectory = (reader.GetInt32(1) & S_IFMT) == S_IFDIR;
            string normalizedPath = NormalizePath(reader.GetString(0), isDirectory);

            // Skip the directory itself (we only queried it to differentiate between a nonexistent and empty
            // directory). The second condition is just to make sure the following code doesn't break if there's a file
            // with the same path as the directory, differing only by leading slash.
            if (normalizedPath == path || !normalizedPath.StartsWith(path))
            {
                continue;
            }

            // Get the name of the directory's direct child (i.e. the first path segment)
            string childName = normalizedPath[path.Length..]; // explicit_dir/, explicit_file, implicit_dir/child_dir/, implicit_dir/child_file
            bool isImplicitDirectory = childName[..^1].Contains('/');

            if (isImplicitDirectory)
            {
                childName = childName[..(childName.IndexOf('/') + 1)]; // implicit_dir/
                normalizedPath = path + childName;
                isDirectory = true;
            }

            // Check if already added
            if (entries.ContainsKey(childName))
            {
                continue;
            }

            // Include metadata for files
            DateTime? dateModified = null;
            string? formattedSize = null;

            if (!isDirectory)
            {
                dateModified = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(2)).UtcDateTime;

                long size = reader.GetInt64(3);
                formattedSize = size.ToString(); // TODO: SizeFormat as a parameter?
                //formattedSize = options.SizeFormat switch
                //{
                //    SizeFormat.Binary => FileSizeFormatter.FormatBytes(size),
                //    SizeFormat.SI => FileSizeFormatter.FormatBytes(size, true),
                //    _ => size.ToString(),
                //};
            }

            // Add to entries
            entries[childName] = new(childName, normalizedPath, dateModified, formattedSize);
        }

        // Sort and return
        // TODO: SortDirectoriesFirst as a parameter?
        return entries.Values/*.Order(comparer)*/.ToList();
    }

    public Stream? GetStream(string path)
    {
        // TODO: Support symlinks.
        //
        // This should be fairly easy to implement for *file* symlinks -- just recursively call GetStream() while
        // keeping track of paths we've already seen (to avoid an infinite loop). The challenge would be supporting
        // *directory* symlinks, as any time we hit a 404 situation (both here and in ListDirectory), we would need to
        // walk up the directory tree, testing for symlinks, then add the rest of the path onto the symlink's target,
        // and do that recursively until we either find a file/directory or run out of symlinks.

        path = NormalizePath(path, isDirectory: false);
        using var _ = logger.BeginTimedOperation($"{nameof(GetStream)} {{Path}}", path);

        // Find row id
        using var sql = connection.CreateCommand();
        sql.CommandText = $"""
            select _rowid_, sz from sqlar
            where
                (name =         $trimmedPath or
                 name =  '/' || $trimmedPath or
                 name = './' || $trimmedPath) and
                (mode & {S_IFMT}) = {S_IFREG}
            limit 1;
            """;
        sql.Parameters.AddWithValue("$trimmedPath", path.Trim('/'));

        using var reader = sql.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        long rowid = reader.GetInt64(0);
        long size = reader.GetInt64(1);

        return GetStream(rowid, size);
    }

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

    public string NormalizePath(string path, bool isDirectory)
    {
        if (path is "" or ".") // Acceptable as input to ListDirectory()
        {
            return "/";
        }

        if (path.StartsWith("./"))
        {
            path = path[2..];
        }

        var str = "/" + path.Trim('/');

        if (isDirectory && str != "/")
        {
            str += "/";
        }

        return str;
    }

    public string ResolveSymlink(string path)
    {
        throw new NotImplementedException();
    }
}

public class RecursiveSymlinkException(string path) : Exception($"A recursive symlink was detected at \"{path}\".")
{ }
