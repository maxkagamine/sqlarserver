// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqliteArchive.Nodes;

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
