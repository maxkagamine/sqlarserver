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

    internal DirectoryNode(string name, Mode mode, Node? parent = null)
        : base(name, mode, parent)
    { }

    /// <summary>
    /// The directory contents.
    /// </summary>
    public IEnumerable<Node> Children => children.Values;

    public override bool IsDirectory => true;

    protected override bool ValidateMode(in Mode mode) => mode.IsDirectory;

    internal Node? FindChild(string name) => children.TryGetValue(name, out Node? node) ? node : null;
    internal void AddChild(Node node) => children.Add(node.Name, node);
}
