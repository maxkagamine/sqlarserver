// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using FluentAssertions;
using SqliteArchive.Helpers;
using Xunit;

namespace SqliteArchive.Tests;

public class FileSizeFormatterTests
{
    [Theory]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KiB")]
    [InlineData(3993, "3.9 KiB")]
    [InlineData(3470, "3.39 KiB")]
    [InlineData(40284, "39.3 KiB")]
    [InlineData(399760, "390 KiB")]
    [InlineData(4088398, "3.9 MiB")]
    [InlineData(3553624, "3.39 MiB")]
    [InlineData(41249931, "39.3 MiB")]
    [InlineData(409353585, "390 MiB")]
    [InlineData(4186519372, "3.9 GiB")]
    [InlineData(3638911042, "3.39 GiB")]
    [InlineData(42239929614, "39.3 GiB")]
    [InlineData(419178070671, "390 GiB")]
    [InlineData(4286995836699, "3.9 TiB")]
    [InlineData(3726244906533, "3.39 TiB")]
    [InlineData(43253687925080, "39.3 TiB")]
    [InlineData(429238344367473, "390 TiB")]
    [InlineData(4331405000000000, "3939 TiB")]
    public void FormatsBinaryUnits(long bytes, string expected)
    {
        FileSizeFormatter.FormatBytes(bytes).Should().Be(expected);
    }

    [Theory]
    [InlineData(1048054, "1023 KiB")]        // 1023.49 KiB
    [InlineData(1048064, "1 MiB")]           // 1023.50 KiB
    [InlineData(1073207050, "1023 MiB")]     // 1023.49 MiB
    [InlineData(1073217536, "1 GiB")]        // 1023.50 MiB
    [InlineData(1098964019446, "1023 GiB")]  // 1023.49 GiB
    [InlineData(1098974756864, "1 TiB")]     // 1023.50 GiB
    public void Rounds1024ToNextBinaryUnit(long bytes, string expected)
    {
        FileSizeFormatter.FormatBytes(bytes).Should().Be(expected);
    }

    [Theory]
    [InlineData(999, "999 B")]
    [InlineData(1000, "1 KB")]
    [InlineData(3899, "3.9 KB")]
    [InlineData(3389, "3.39 KB")]
    [InlineData(39339, "39.3 KB")]
    [InlineData(390390, "390 KB")]
    [InlineData(3899000, "3.9 MB")]
    [InlineData(3389000, "3.39 MB")]
    [InlineData(39339000, "39.3 MB")]
    [InlineData(390390000, "390 MB")]
    [InlineData(3899000000, "3.9 GB")]
    [InlineData(3389000000, "3.39 GB")]
    [InlineData(39339000000, "39.3 GB")]
    [InlineData(390390000000, "390 GB")]
    [InlineData(3899000000000, "3.9 TB")]
    [InlineData(3389000000000, "3.39 TB")]
    [InlineData(39339000000000, "39.3 TB")]
    [InlineData(390390000000000, "390 TB")]
    [InlineData(3939390000000000, "3939 TB")]
    public void FormatsSIUnits(long bytes, string expected)
    {
        FileSizeFormatter.FormatBytes(bytes, true).Should().Be(expected);
    }

    [Theory]
    [InlineData(999490, "999 KB")]        // 999.49 KB
    [InlineData(999500, "1 MB")]          // 999.50 KB
    [InlineData(999490000, "999 MB")]     // 999.49 MB
    [InlineData(999500000, "1 GB")]       // 999.50 MB
    [InlineData(999490000000, "999 GB")]  // 999.49 GB
    [InlineData(999500000000, "1 TB")]    // 999.50 GB
    public void Rounds1000ToNextSIUnit(long bytes, string expected)
    {
        FileSizeFormatter.FormatBytes(bytes, true).Should().Be(expected);
    }
}
