// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqliteArchive.Nodes;

/// <summary>
/// Represents a node in the Sqlar virtual filesystem.
/// </summary>
public abstract class Node
{
    private Mode mode;

    /// <summary>
    /// The file or directory name, without slashes. Empty string if root.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The Unix file mode.
    /// </summary>
    public required Mode Mode
    {
        get => mode;
        set
        {
            if (!ValidateMode(value))
            {
                throw new ArgumentException($"{(int)value} ({value}) is not valid for {GetType().Name}.");
            }

            mode = value;
        }
    }

    /// <summary>
    /// The parent node, or <see langword="null"/> if the root node.
    /// </summary>
    public Node? Parent { get; set; }

    /// <summary>
    /// Whether this node is a directory, or a symlink that eventually points to one.
    /// </summary>
    public abstract bool IsDirectory { get; }

    /// <summary>
    /// Validates the assigned mode to ensure that e.g. a <see cref="FileNode"/> isn't created for a directory.
    /// </summary>
    protected abstract bool ValidateMode(in Mode mode);
}
