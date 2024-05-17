// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

namespace SqliteArchive;

[Flags]
public enum Permissions
{
    SetUid = 0b100000000000,
    SetGid = 0b010000000000,
    Sticky = 0b001000000000,

    UserRead = 0b000100000000,
    UserWrite = 0b000010000000,
    UserExecute = 0b000001000000,

    GroupRead = 0b000000100000,
    GroupWrite = 0b000000010000,
    GroupExecute = 0b000000001000,

    OtherRead = 0b000000000100,
    OtherWrite = 0b000000000010,
    OtherExecute = 0b000000000001,

    UserAll = UserRead | UserWrite | UserExecute,
    GroupAll = GroupRead | GroupWrite | GroupExecute,
    OtherAll = OtherRead | OtherWrite | OtherExecute,

    /// <summary>
    /// All permissions for user, group, and other (0777).
    /// </summary>
    All = UserAll | GroupAll | OtherAll,

    /// <summary>
    /// Full permissions mask including the special bits (7777).
    /// </summary>
    Mask = SetUid | SetGid | Sticky | All
}
