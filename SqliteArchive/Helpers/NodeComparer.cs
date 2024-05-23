// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using NaturalSort.Extension;
using SqliteArchive.Nodes;

namespace SqliteArchive.Helpers;

public class NodeComparer : IComparer<Node>
{
    private static readonly NaturalSortComparer naturalSortComparer = new(StringComparison.OrdinalIgnoreCase);

    public bool DirectoriesFirst { get; set; } = true;

    public int Compare(Node? x, Node? y)
    {
        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        if (DirectoriesFirst)
        {
            if (x.IsDirectory && !y.IsDirectory)
            {
                return -1;
            }

            if (y.IsDirectory && !x.IsDirectory)
            {
                return 1;
            }
        }

        return naturalSortComparer.Compare(x.Name, y.Name);
    }
}
