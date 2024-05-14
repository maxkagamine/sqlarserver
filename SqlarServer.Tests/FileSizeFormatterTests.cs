// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using FluentAssertions;
using SqlarServer.Services;
using Xunit;

namespace SqlarServer.Tests;

public class FileSizeFormatterTests
{
    [Theory]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KiB")]
    [InlineData(40335, "39 KiB")]
    [InlineData(1048576, "1 MiB")]
    [InlineData(41300263, "39.39 MiB")]
    [InlineData(1073741824, "1 GiB")]
    [InlineData(42291469221, "39.39 GiB")]
    [InlineData(1099511627776, "1 TiB")]
    [InlineData(43306464483213, "39.39 TiB")]
    public void FormatsBinaryUnits(long bytes, string expected)
    {
        FileSizeFormatter.FormatBytes(bytes).Should().Be(expected);
    }

    [Theory]
    [InlineData(1048063, "1023 KiB")]          // 1023.499 KiB
    [InlineData(1048064, "1 MiB")]             // 1023.500 KiB
    [InlineData(1073735680, "1023.99 MiB")]    // 1023.994 MiB
    [InlineData(1073736681, "1 GiB")]          // 1023.995 MiB
    [InlineData(1099506259066, "1023.99 GiB")] // 1023.994 GiB
    [InlineData(1099506259067, "1 TiB")]       // 1023.995 GiB
    public void Rounds1024ToNextBinaryUnit(long bytes, string expected)
    {
        FileSizeFormatter.FormatBytes(bytes).Should().Be(expected);
    }

    [Theory]
    [InlineData(999, "999 B")]
    [InlineData(1000, "1 KB")]
    [InlineData(39387, "39 KB")]
    [InlineData(1000000, "1 MB")]
    [InlineData(39387000, "39.39 MB")]
    [InlineData(1000000000, "1 GB")]
    [InlineData(39387000000, "39.39 GB")]
    [InlineData(1000000000000, "1 TB")]
    [InlineData(39387000000000, "39.39 TB")]
    public void FormatsSIUnits(long bytes, string expected)
    {
        FileSizeFormatter.FormatBytes(bytes, true).Should().Be(expected);
    }

    [Theory]
    [InlineData(999499, "999 KB")]          // 999.499 KB
    [InlineData(999500, "1 MB")]            // 999.500 KB
    [InlineData(999994999, "999.99 MB")]    // 999.994 MB
    [InlineData(999995000, "1 GB")]         // 999.995 MB
    [InlineData(999994999999, "999.99 GB")] // 999.994 GB
    [InlineData(999995000000, "1 TB")]      // 999.995 GB
    public void Rounds1000ToNextSIUnit(long bytes, string expected)
    {
        FileSizeFormatter.FormatBytes(bytes, true).Should().Be(expected);
    }
}
