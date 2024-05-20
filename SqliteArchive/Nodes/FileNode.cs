// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqliteArchive.Nodes;

/// <summary>
/// Represents a regular file in the Sqlar virtual filesystem.
/// </summary>
public class FileNode : Node
{
    internal FileNode(string name, Mode mode, Node parent, long rowId, DateTime dateModified, long size)
        : base(name, mode, dateModified, size, parent)
    {
        RowId = rowId;
    }

    /// <summary>
    /// Table rowid for efficient blob retreival.
    /// </summary>
    public long RowId { get; }

    public override bool IsDirectory => false;

    protected override bool ValidateMode(in Mode mode) => mode.IsRegularFile;
}
