// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using System.Diagnostics;

namespace SqliteArchive.Nodes;

/// <summary>
/// Represents a node in the Sqlar virtual filesystem.
/// </summary>
[DebuggerDisplay("{GetType().Name,nq} {Path}")]
public class Node
{
    private Mode mode;

    internal Node(string name, Mode mode, DateTime dateModified, long size, long compressedSize, DirectoryNode? parent)
    {
        Name = name;
        Path = parent?.Path + name;
        Mode = mode;
        DateModified = dateModified;
        Size = size;
        CompressedSize = compressedSize;
        Parent = parent;
    }

    /// <summary>
    /// The file or directory name. Empty string if root.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The node's absolute path.
    /// </summary>
    /// <remarks>
    /// This is the node's "real path," i.e. the resolved absolute path as would be returned by the <c>realpath</c>
    /// command.
    /// </remarks>
    public Path Path { get; }

    /// <summary>
    /// The Unix file mode.
    /// </summary>
    public Mode Mode
    {
        get => mode;
        internal set
        {
            if (!ValidateMode(value))
            {
                throw new ArgumentException($"{(int)value} ({value}) is not valid for {GetType().Name}.");
            }

            mode = value;
        }
    }

    /// <summary>
    /// The modified time as a UTC date.
    /// </summary>
    public virtual DateTime DateModified { get; internal set; }

    /// <summary>
    /// The file size in bytes.
    /// </summary>
    public long Size { get; }

    /// <summary>
    /// The size of the stored blob in bytes. Data is stored uncompressed if equal to <see cref="Size"/>.
    /// </summary>
    public long CompressedSize { get; }

    /// <summary>
    /// Compression ratio. Zero if uncompressed (or no data); close to 1 if highly compressed.
    /// </summary>
    public double CompressionRatio => Size == 0 ? 0 : 1 - ((double)CompressedSize / Size);

    /// <summary>
    /// The parent directory, or <see langword="null"/> if the current node is the root.
    /// </summary>
    public DirectoryNode? Parent { get; }

    /// <summary>
    /// Whether this node is a directory, or a symlink that eventually points to one.
    /// </summary>
    public virtual bool IsDirectory => false;

    /// <summary>
    /// Validates the assigned mode to ensure that e.g. a <see cref="FileNode"/> isn't created for a directory.
    /// </summary>
    protected virtual bool ValidateMode(in Mode mode)
        => !mode.IsDirectory && !mode.IsRegularFile && !mode.IsSymbolicLink;
}
