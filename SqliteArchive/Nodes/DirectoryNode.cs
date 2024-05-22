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

    private readonly Lazy<long> totalSize;
    private readonly Lazy<long> totalCompressedSize;
    private readonly Lazy<double> totalCompressionRatio;

    internal DirectoryNode(string name, Mode mode, DateTime dateModified, Node? parent)
        : base(name, mode, dateModified, size: 0, compressedSize: 0, parent)
    {
        // These properties must not be accessed during initialization / before all nodes have been added to the tree
        totalSize = new(() => Children.Sum(n => n is DirectoryNode d ? d.TotalSize : n.Size));
        totalCompressedSize = new(() => Children.Sum(n => n is DirectoryNode d ? d.TotalCompressedSize : n.CompressedSize));
        totalCompressionRatio = new(() => totalSize.Value == 0 ? 0 : 1 - ((double)totalCompressedSize.Value / totalSize.Value));
    }

    /// <summary>
    /// The directory contents.
    /// </summary>
    public IEnumerable<Node> Children => children.Values;

    /// <summary>
    /// Total size in bytes, recursive.
    /// </summary>
    public long TotalSize => totalSize.Value;

    /// <summary>
    /// Total size of the stored blobs in bytes, recursive. Data is stored uncompressed if equal to <see
    /// cref="TotalSize"/>.
    /// </summary>
    public long TotalCompressedSize => totalCompressedSize.Value;

    /// <summary>
    /// Total compression ratio, recursive. Zero if uncompressed (or no data); close to 1 if highly compressed.
    /// </summary>
    public double TotalCompressionRatio => totalCompressionRatio.Value;

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
