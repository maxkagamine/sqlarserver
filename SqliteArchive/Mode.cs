// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using System.Diagnostics;

namespace SqliteArchive;

/// <summary>
/// Represents a Unix file mode.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct Mode
{
    // https://github.com/torvalds/linux/blob/master/include/uapi/linux/stat.h
    private const int S_IFMT = 0b1111000000000000;
    private const int S_IFDIR = 0b0100000000000000;
    private const int S_IFREG = 0b1000000000000000;
    private const int S_IFLNK = 0b1010000000000000;

    /// <summary>
    /// Represents a directory with 777 permissions.
    /// </summary>
    public static readonly Mode Directory = new(S_IFDIR | (int)Permissions.All);

    /// <summary>
    /// Represents a regular file with 777 permissions.
    /// </summary>
    public static readonly Mode RegularFile = new(S_IFREG | (int)Permissions.All);

    /// <summary>
    /// Represents a symlink with 777 permissions.
    /// </summary>
    public static readonly Mode SymbolicLink = new(S_IFLNK | (int)Permissions.All);

    private readonly int mode;

    public Mode(int mode) => this.mode = mode;

    public bool IsDirectory => (mode & S_IFMT) == S_IFDIR;
    public bool IsRegularFile => (mode & S_IFMT) == S_IFREG;
    public bool IsSymbolicLink => (mode & S_IFMT) == S_IFLNK;

    public Permissions Permissions => (Permissions)(mode & (int)Permissions.Mask);

    /// <summary>
    /// Changes the permission bits and returns the new <see cref="Mode"/>.
    /// </summary>
    public Mode With(Permissions permissions) => new((mode & ~(int)Permissions.Mask) | (int)permissions);

    public override string ToString()
    {
        Span<char> str = stackalloc char[10];
        var perms = Permissions;

        str[0] = IsDirectory ? 'd' :
                 IsRegularFile ? '-' :
                 IsSymbolicLink ? 'l' :
                 '?';

        // Using bitwise is faster than HasFlag / avoids boxing perms over and over
        str[1] = (perms & Permissions.UserRead) == Permissions.UserRead ? 'r' : '-';
        str[2] = (perms & Permissions.UserWrite) == Permissions.UserWrite ? 'w' : '-';

        if ((perms & Permissions.SetUid) == Permissions.SetUid)
        {
            str[3] = (perms & Permissions.UserExecute) == Permissions.UserExecute ? 's' : 'S';
        }
        else
        {
            str[3] = (perms & Permissions.UserExecute) == Permissions.UserExecute ? 'x' : '-';
        }

        str[4] = (perms & Permissions.GroupRead) == Permissions.GroupRead ? 'r' : '-';
        str[5] = (perms & Permissions.GroupWrite) == Permissions.GroupWrite ? 'w' : '-';

        if ((perms & Permissions.SetGid) == Permissions.SetGid)
        {
            str[6] = (perms & Permissions.GroupExecute) == Permissions.GroupExecute ? 's' : 'S';
        }
        else
        {
            str[6] = (perms & Permissions.GroupExecute) == Permissions.GroupExecute ? 'x' : '-';
        }

        str[7] = (perms & Permissions.OtherRead) == Permissions.OtherRead ? 'r' : '-';
        str[8] = (perms & Permissions.OtherWrite) == Permissions.OtherWrite ? 'w' : '-';

        if ((perms & Permissions.Sticky) == Permissions.Sticky)
        {
            str[9] = (perms & Permissions.OtherExecute) == Permissions.OtherExecute ? 't' : 'T';
        }
        else
        {
            str[9] = (perms & Permissions.OtherExecute) == Permissions.OtherExecute ? 'x' : '-';
        }

        return new string(str);
    }

    public static implicit operator Mode(int mode) => new(mode);
    public static implicit operator int(Mode mode) => mode.mode;
}
