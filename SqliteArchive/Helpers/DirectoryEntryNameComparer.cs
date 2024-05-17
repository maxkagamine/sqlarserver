// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using NaturalSort.Extension;

namespace SqliteArchive.Helpers;

public class DirectoryEntryNameComparer : IComparer<DirectoryEntry>
{
    private static readonly NaturalSortComparer naturalSortComparer = new(StringComparison.OrdinalIgnoreCase);

    public bool SortDirectoriesFirst { get; set; } = true;

    public int Compare(DirectoryEntry? x, DirectoryEntry? y)
    {
        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        if (SortDirectoriesFirst)
        {
            if (x.Name.EndsWith('/') && !y.Name.EndsWith('/'))
            {
                return -1;
            }

            if (y.Name.EndsWith('/') && !x.Name.EndsWith('/'))
            {
                return 1;
            }
        }

        return naturalSortComparer.Compare(x.Name, y.Name);
    }
}
