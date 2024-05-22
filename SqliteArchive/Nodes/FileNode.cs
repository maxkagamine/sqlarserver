// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqliteArchive.Nodes;

/// <summary>
/// Represents a regular file in the Sqlar virtual filesystem.
/// </summary>
public class FileNode : Node
{
    internal FileNode(string name, Mode mode, DateTime dateModified, long size, long compressedSize, Node parent, long rowId)
        : base(name, mode, dateModified, size, compressedSize, parent)
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
