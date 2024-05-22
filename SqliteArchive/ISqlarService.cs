// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using SqliteArchive.Nodes;

namespace SqliteArchive;

public interface ISqlarService
{
    /// <summary>
    /// Looks for the given path in the archive.
    /// </summary>
    /// <remarks>
    /// A directory may exist in the archive either due to an explicit directory entry or through the implied hierarchy
    /// of files, like a cloud storage bucket. That is, if files exist under <paramref name="path"/>, this method will
    /// return a <see cref="DirectoryNode"/> even if no row for <paramref name="path"/> itself exists.
    /// <para/> Due to the flat nature of the sqlar table, it's possible for a file to exist at both "/foo" and
    /// "/foo/bar". The behavior when looking up /foo in this case is undefined. Please don't do that.
    /// <para /> The <a href="https://www.sqlite.org/sqlar.html">SQLite Archive spec</a> describes the 'name' field as
    /// "the full pathname relative to the root of the archive;" however, it's possible that names may lead with a slash
    /// or ./ (dot slash) depending on how the archive was created. This class treats these as the same.
    /// </remarks>
    /// <param name="path">Path with or without leading slash.</param>
    /// <param name="dereference">Whether to dereference <paramref name="path"/> if it's a symlink. Symlinks in the
    /// directory path will be followed regardless.</param>
    /// <returns>A <see cref="DirectoryNode"/>, <see cref="FileNode"/>, <see cref="SymbolicLinkNode"/> (if <paramref
    /// name="dereference"/> is <see langword="false"/>, or if the symlink is broken), or <see cref="Node"/> (as a
    /// fallback) if found; otherwise <see langword="null"/>.</returns>
    Node? FindPath(Path path, bool dereference = false);

    /// <inheritdoc cref="FindPath(Path)"/>
    Node? FindPath(string path, bool dereference = false);

    /// <summary>
    /// Returns the uncompressed data stream for the given file.
    /// </summary>
    /// <param name="file">The file node.</param>
    Stream GetStream(FileNode file);
}
