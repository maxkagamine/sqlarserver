// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqliteArchive.Nodes;

/// <summary>
/// Represents a directory in the Sqlar virtual filesystem.
/// </summary>
public class DirectoryNode : Node
{
    // TODO: We could add case-insensitive support by setting this dictionary to OrdinalIgnoreCase
    private readonly Dictionary<string, Node> children = [];

    internal DirectoryNode(string name, Mode mode, DateTime dateModified, Node? parent)
        : base(name, mode, dateModified, size: 0, parent)
    { }

    /// <summary>
    /// The directory contents.
    /// </summary>
    public IEnumerable<Node> Children => children.Values;

    // TODO: Add a unit test for automatic date modified of implicit directories
    public override DateTime DateModified
    {
        get => IsImplicit ? Children.Select(n => n.DateModified).Max() : base.DateModified;
    }

    public override bool IsDirectory => true;

    protected override bool ValidateMode(in Mode mode) => mode.IsDirectory;

    /// <summary>
    /// Whether the directory was created implicitly in order to add a descendent node.
    /// </summary>
    internal bool IsImplicit { get; set; }

    internal Node? FindChild(string name) => children.TryGetValue(name, out Node? node) ? node : null;
    internal void AddChild(Node node) => children.Add(node.Name, node);
}
