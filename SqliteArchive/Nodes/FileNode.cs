// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqliteArchive.Nodes;

/// <summary>
/// Represents a regular file in the Sqlar virtual filesystem.
/// </summary>
public class FileNode : Node
{
    internal FileNode(string name, Mode mode, Node parent, long rowId, DateTime dateModified, long size) : base(name, mode, parent)
    {
        RowId = rowId;
        DateModified = dateModified;
        Size = size;
    }

    /// <summary>
    /// Table rowid for efficient blob retreival.
    /// </summary>
    public long RowId { get; }

    /// <summary>
    /// The file's mtime as a UTC date.
    /// </summary>
    public DateTime DateModified { get; }

    /// <summary>
    /// The file size.
    /// </summary>
    public long Size { get; }

    public override bool IsDirectory => false;

    protected override bool ValidateMode(in Mode mode) => mode.IsRegularFile;
}
