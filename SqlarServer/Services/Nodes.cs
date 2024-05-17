// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqlarServer.Services;

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

/// <summary>
/// Represents a directory in the Sqlar virtual filesystem.
/// </summary>
public class DirectoryNode : Node
{
    /// <summary>
    /// The directory contents.
    /// </summary>
    public List<Node> Children { get; } = [];

    public override bool IsDirectory => true;

    protected override bool ValidateMode(in Mode mode) => mode.IsDirectory;
}

/// <summary>
/// Represents a regular file in the Sqlar virtual filesystem.
/// </summary>
public class FileNode : Node
{
    /// <summary>
    /// Table rowid for efficient blob retreival.
    /// </summary>
    public long RowId { get; set; }

    /// <summary>
    /// The file's mtime as a UTC date.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// The file size.
    /// </summary>
    public long Size { get; set; }

    public override bool IsDirectory => false;

    protected override bool ValidateMode(in Mode mode) => mode.IsRegularFile;
}

/// <summary>
/// Represents a symlink in the Sqlar virtual filesystem.
/// </summary>
public class SymbolicLinkNode : Node
{
    /// <summary>
    /// The raw target string.
    /// </summary>
    public required string Target { get; set; }

    /// <summary>
    /// The symlink's target node, if successfully resolved.
    /// </summary>
    public Node? TargetNode { get; set; }

    /// <summary>
    /// Whether this node is a broken symlink (target does not exist).
    /// </summary>
    public bool IsBroken => TargetNode is null;

    public override bool IsDirectory => TargetNode?.IsDirectory == true;

    protected override bool ValidateMode(in Mode mode) => mode.IsSymbolicLink;
}
