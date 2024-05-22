// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SqliteArchive.Nodes;

/// <summary>
/// Represents a symlink in the Sqlar virtual filesystem.
/// </summary>
public class SymbolicLinkNode : Node
{
    private Node? targetNode;

    internal SymbolicLinkNode(string name, Mode mode, DateTime dateModified, Node parent, string target)
        : base(name, mode, dateModified, size: Encoding.UTF8.GetByteCount(target), parent)
    {
        Target = target;
    }

    /// <summary>
    /// The raw target string.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// The symlink's dereferenced target node, or <see langword="null"/> if the target does not exist or is recursive.
    /// </summary>
    public Node? TargetNode
    {
        get => targetNode;
        internal set
        {
            if (value is SymbolicLinkNode)
            {
                throw new ArgumentException($"Should not attempt to assign a {nameof(SymbolicLinkNode)} as a symlink's {nameof(TargetNode)}.");
            }

            targetNode = value;
        }
    }

    /// <summary>
    /// Whether this node is a broken symlink (target does not exist or is recursive).
    /// </summary>
    [MemberNotNullWhen(false, nameof(TargetNode))]
    public bool IsBroken => TargetNode is null;

    public override bool IsDirectory => TargetNode?.IsDirectory == true;

    /// <summary>
    /// Used to track whether <see cref="TargetNode"/> still needs to be resolved and to avoid recursive loops.
    /// </summary>
    internal SymlinkResolutionState ResolutionState { get; set; }

    protected override bool ValidateMode(in Mode mode) => mode.IsSymbolicLink;
}

internal enum SymlinkResolutionState
{
    NotResolved,
    StartedResolving,
    Resolved
}
