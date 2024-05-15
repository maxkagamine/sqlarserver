// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using SqlarServer.Models;

namespace SqlarServer.Services;

public class SqlarService : ISqlarService
{
    // https://github.com/torvalds/linux/blob/master/include/uapi/linux/stat.h
    private const int S_IFMT = 0xF000;
    private const int S_IFDIR = 0x4000;

    private readonly SqliteConnection connection;
    private readonly SqlarOptions options;
    private readonly DirectoryEntryNameComparer comparer;

    public SqlarService(SqliteConnection connection, IOptions<SqlarOptions> options)
    {
        this.connection = connection;
        this.options = options.Value;

        comparer = new(); // TODO: Add option for sorting directories first or not
    }

    public IEnumerable<DirectoryEntry>? ListDirectory(string path)
    {
        path = NormalizePath(path, true);

        // Search the db for an explicit directory entry matching the given path, if present (as the directory may exist
        // but be empty), and any and all descendents (not only direct children, as its children may include implicit
        // directories; e.g. "foo" implicitly contains a directory "bar" if a row "foo/bar/blah/stuff" exists)
        using var sql = connection.CreateCommand();

        if (path == "/")
        {
            sql.CommandText = $"select name, mode, mtime, sz from {options.TableName};";
        }
        else
        {
            sql.CommandText = $"""
                select name, mode, mtime, sz from {options.TableName}
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
                dateModified = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt32(2)).UtcDateTime;

                long size = reader.GetInt64(3);
                formattedSize = options.SizeFormat switch
                {
                    SizeFormat.Binary => FileSizeFormatter.FormatBytes(size),
                    SizeFormat.SI => FileSizeFormatter.FormatBytes(size, true),
                    _ => size.ToString(),
                };
            }

            // Add to entries
            entries[childName] = new(childName, normalizedPath, dateModified, formattedSize);
        }

        // Sort and return
        return entries.Values.Order(comparer).ToList();
    }

    public Stream? GetStream(string path)
    {
        throw new NotImplementedException();
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
}
