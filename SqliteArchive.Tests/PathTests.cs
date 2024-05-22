// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using Xunit;

namespace SqliteArchive.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Keeps tests cleaner")]
public class PathTests
{
    [Theory]
    [InlineData("", new string[] { })]
    [InlineData(".", new string[] { })]
    [InlineData("/", new string[] { })]
    [InlineData("./", new string[] { })]
    [InlineData("foo/bar", new string[] { "foo", "bar" })]
    [InlineData("/foo/bar", new string[] { "foo", "bar" })]
    [InlineData("./foo/bar", new string[] { "foo", "bar" })]
    public void ParsesPathsIntoSegments(string str, string[] expected)
    {
        Assert.Equal(expected, new Path(str).ToArray());
    }

    [Theory]
    [InlineData("/", ".", new string[] { })]
    [InlineData("/", "foo", new string[] { "foo" })]
    [InlineData("/foo", ".", new string[] { "foo" })]
    [InlineData("/foo", "..", new string[] { })]
    [InlineData("/foo", "bar", new string[] { "foo", "bar" })]
    [InlineData("/foo", "bar/stuff", new string[] { "foo", "bar", "stuff" })]
    [InlineData("/foo/bar", "../things", new string[] { "foo", "things" })]
    [InlineData("/foo/bar", "../../blah", new string[] { "blah" })]
    [InlineData("/foo/bar", "../../../../../blah", new string[] { "blah" })]
    [InlineData("/foo/bar", "/blah", new string[] { "blah" })]
    [InlineData("/foo/bar", "./blah", new string[] { "foo", "bar", "blah" })]
    public void ResolvesRelativePaths(string baseStr, string relativeStr, string[] expected)
    {
        Assert.Equal(expected, new Path(baseStr, relativeStr).ToArray());
    }

    [Fact]
    public void IsRoot()
    {
        Assert.True(Path.Root.IsRoot);
        Assert.True(new Path("/").IsRoot);
        Assert.True(new Path("").IsRoot);
        Assert.False(new Path("foo").IsRoot);
    }

    [Fact]
    public void Parent()
    {
        var path = new Path("foo/bar");

        Assert.NotNull(path.Parent);
        Assert.Equal(["foo"], path.Parent.ToArray());
        Assert.NotNull(path.Parent.Parent);
        Assert.Equal([], path.Parent.Parent.ToArray());
        Assert.Null(Path.Root.Parent);
    }

    [Fact]
    public void BaseName()
    {
        Assert.Equal("foo", new Path("foo").BaseName);
        Assert.Equal("stuff", new Path("foo/bar/stuff").BaseName);
        Assert.Null(Path.Root.BaseName);
    }

    [Fact]
    public void String()
    {
        Assert.Equal("/foo/bar", new Path("foo/bar").ToString());
        Assert.Equal("/foo/bar", new Path("foo/bar").ToString(trailingSlash: false));
        Assert.Equal("/foo/bar/", new Path("foo/bar").ToString(trailingSlash: true));
        Assert.Equal("/", Path.Root.ToString());
        Assert.Equal("/", Path.Root.ToString(trailingSlash: false));
        Assert.Equal("/", Path.Root.ToString(trailingSlash: true));
    }

    [Fact]
    public void Equality()
    {
        var fooPath1 = new Path("foo");
        var fooPath2 = new Path("/foo");
        var barPath = new Path("bar");
        var rootPath = new Path("");

        Assert.True(fooPath1 == fooPath2);
        Assert.False(fooPath1 == barPath);
        Assert.True(fooPath1 != barPath);
        Assert.True(rootPath == Path.Root);
    }

    [Fact]
    public void Addition()
    {
        var foo = new Path("foo");
        var fooPlusBar = foo + "bar";
        var fooPlusUpdog = foo + "../dog"; // what's up dog lmao gottem
        var nullPlusFoo = (Path?)null + "foo";
        var nullPlusEmptyString = (Path?)null + "";

        Assert.Equal(["foo", "bar"], fooPlusBar.ToArray());
        Assert.Equal(["dog"], fooPlusUpdog.ToArray());
        Assert.Equal(["foo"], nullPlusFoo.ToArray());
        Assert.Equal([], nullPlusEmptyString.ToArray());
    }
}
