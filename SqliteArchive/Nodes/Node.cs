// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using System.Diagnostics;

namespace SqliteArchive.Nodes;

/// <summary>
/// Represents a node in the Sqlar virtual filesystem.
/// </summary>
[DebuggerDisplay("{Path,nq}", Name = "{GetType().Name,nq}")]
public class Node
{
    private Mode mode;

    internal Node(string name, Mode mode, DateTime dateModified, long size, Node? parent)
    {
        Name = name;
        Mode = mode;
        DateModified = dateModified;
        Size = size;
        Parent = parent;
    }

    /// <summary>
    /// The file or directory name. Empty string if root.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The node's absolute path.
    /// </summary>
    public string Path => $"{Parent?.Path}/{Name}";

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
    /// The parent node, or <see langword="null"/> if the root node.
    /// </summary>
    public Node? Parent { get; }

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
