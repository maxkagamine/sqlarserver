// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using SqlarServer.Models;

namespace SqlarServer.Services;
public interface ISqlarService
{
    /// <summary>
    /// Looks for a directory (or implied directory) matching the given path and returns its children, including implied
    /// subdirectories, or <see langword="null"/> if no such path exists. A "directory" may exist in the archive either
    /// due to the presence of an actual row with the directory bit set, or through the implied hierarchy of files even
    /// if the path segments don't exist on their own (like a cloud storage bucket).
    /// </summary>
    /// <remarks>
    /// Due to the flat nature of the sqlar table, it's possible for a file to exist at both "/foo" and "/foo/bar". The
    /// behavior when looking up /foo in this case is undefined. Please don't do that.
    /// <para />
    /// The <a href="https://www.sqlite.org/sqlar.html">SQLite Archive spec</a> describes the 'name' field as "the full
    /// pathname relative to the root of the archive;" however, it's possible that names may lead with a slash or ./
    /// (dot slash) depending on how the archive was created. This class treats these as the same.
    /// </remarks>
    /// <param name="path">The directory path to search.</param>
    /// <returns>The directory contents, if it exists in the archive; otherwise <see langword="null"/>. May be empty if
    /// only a directory node exists with no descendants.</returns>
    IEnumerable<DirectoryEntry>? ListDirectory(string path);

    /// <summary>
    /// Looks for a path in the archive and returns the blob stream if it refers to a regular file.
    /// </summary>
    /// <remarks>
    /// Ignores leading slash or ./ (dot slash) as described in <see cref="ListDirectory(string)"/>'s remarks. If two
    /// files have the same path due to inconsistent path type, which file is returned is undefined.
    /// </remarks>
    /// <param name="path">The file path to search.</param>
    /// <returns>The blob stream if found (and not a directory); otherwise <see langword="null"/>.</returns>
    Stream? GetStream(string path);

    /// <summary>
    /// Ensures paths start with a leading slash and that directories end in a trailing slash.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <param name="isDirectory">Whether this path is of a directory.</param>
    string NormalizePath(string path, bool isDirectory);
}
