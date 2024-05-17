// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqliteArchive.Nodes;

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
