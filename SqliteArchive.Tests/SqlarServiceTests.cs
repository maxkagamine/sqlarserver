// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteArchive.Server;
using System.Text;
using Xunit;

namespace SqliteArchive.Tests;

public sealed class SqlarServiceTests : IDisposable
{
    private static readonly ServerOptions DefaultOptions = new()
    {
        TableName = "sqlar",
        SizeFormat = SizeFormat.Binary,
        SortDirectoriesFirst = true,
    };

    // File modes. Can be shown with `stat --format=%f`. Only the S_IFMT bits are actually relevant here:
    // https://github.com/torvalds/linux/blob/master/include/uapi/linux/stat.h
    private const int Directory = 0x41ff;
    private const int RegularFile = 0x81a4;
    private const int Symlink = 0xa1ff;

    private readonly SqliteConnection connection;

    public SqlarServiceTests()
    {
        connection = new("Data Source=:memory:");
        connection.Open();
    }

    private SqlarService CreateService(
        IEnumerable<(string Name, int Mode, DateTime DateModified, byte[] Data)> rows,
        ServerOptions? options = null)
    {
        options ??= DefaultOptions;

        // Create table
        using var createTable = connection.CreateCommand();
        createTable.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {options.TableName} (
              name TEXT PRIMARY KEY,  -- name of the file
              mode INT,               -- access permissions
              mtime INT,              -- last modification time
              sz INT,                 -- original file size
              data BLOB               -- compressed content
            )
            """;
        createTable.ExecuteNonQuery();

        // Insert rows
        foreach (var (name, mode, mtime, data) in rows)
        {
            using var insert = connection.CreateCommand();
            insert.CommandText = $"""
                INSERT INTO {options.TableName}(name, mode, mtime, sz, data)
                VALUES($name, $mode, $mtime, $sz, zeroblob($sz));
                SELECT last_insert_rowid();
                """;
            insert.Parameters.AddWithValue("$name", name);
            insert.Parameters.AddWithValue("$mode", mode);
            insert.Parameters.AddWithValue("$mtime", new DateTimeOffset(mtime).ToUnixTimeSeconds());
            insert.Parameters.AddWithValue("$sz", data.Length);
            var rowId = (long)insert.ExecuteScalar()!;

            if (data.Length > 0)
            {
                using var blob = new SqliteBlob(connection, options.TableName, "data", rowId);
                blob.Write(data);
            }
        }

        // Instantiate service
        return new SqlarService(connection, NullLogger<SqlarService>.Instance);
    }

    public void Dispose() => connection.Dispose();

    [Fact]
    public void ListDirectory_FindsExplicitDirectories()
    {
        var service = CreateService([
            ("dir 1", Directory, DateTime.Now, []),
            ("dir 1/child dir", Directory, DateTime.Now, []),
            ("dir 1/child file", RegularFile, DateTime.Now, []),
            ("dir 2", Directory, DateTime.Now, []),
            ("file 1", RegularFile, DateTime.Now, []),
        ]);

        var root = service.ListDirectory("/");

        Assert.NotNull(root);
        Assert.Collection(root,
            x => Assert.Equal(("dir 1/", "/dir 1/"), (x.Name, x.Path)),
            x => Assert.Equal(("dir 2/", "/dir 2/"), (x.Name, x.Path)),
            x => Assert.Equal(("file 1", "/file 1"), (x.Name, x.Path)));

        var dir1 = service.ListDirectory("/dir 1");

        Assert.NotNull(dir1);
        Assert.Collection(dir1,
            x => Assert.Equal(("child dir/", "/dir 1/child dir/"), (x.Name, x.Path)),
            x => Assert.Equal(("child file", "/dir 1/child file"), (x.Name, x.Path)));

        var childDir = service.ListDirectory("/dir 1/child dir");

        Assert.NotNull(childDir);
        Assert.Empty(childDir);
    }

    [Fact]
    public void ListDirectory_FindsImplicitDirectories()
    {
        var service = CreateService([
            ("dir 1/child dir/another file", RegularFile, DateTime.Now, []),
            ("dir 1/child file", RegularFile, DateTime.Now, []),
            ("file 1", RegularFile, DateTime.Now, []),
        ]);

        var root = service.ListDirectory("/");

        Assert.NotNull(root);
        Assert.Collection(root,
            x => Assert.Equal(("dir 1/", "/dir 1/"), (x.Name, x.Path)),
            x => Assert.Equal(("file 1", "/file 1"), (x.Name, x.Path)));

        var dir1 = service.ListDirectory("/dir 1");

        Assert.NotNull(dir1);
        Assert.Collection(dir1,
            x => Assert.Equal(("child dir/", "/dir 1/child dir/"), (x.Name, x.Path)),
            x => Assert.Equal(("child file", "/dir 1/child file"), (x.Name, x.Path)));
    }

    [Fact]
    public void ListDirectory_ReturnsNullIfNotFound()
    {
        var service = CreateService([
            ("foo", Directory, DateTime.Now, []),
        ]);

        Assert.NotNull(service.ListDirectory("foo"));
        Assert.Null(service.ListDirectory("bar"));
    }

    [Fact]
    public void ListDirectory_ReturnsNullIfFile()
    {
        var service = CreateService([
            ("foo", RegularFile, DateTime.Now, []),
        ]);

        Assert.Null(service.ListDirectory("foo"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ListDirectory_EmptyArchiveIsEmpty(bool includeRootInArchive)
    {
        var service = CreateService(includeRootInArchive ?
            [(".", Directory, DateTime.Now, [])] : // This can happen if running `sqlite3 -Acf <db> .` in an empty dir
            []);
        var root = service.ListDirectory("/");

        Assert.NotNull(root);
        Assert.Empty(root);
    }

    [Fact]
    public void ListDirectory_FilesIncludeDateModified()
    {
        var date = new DateTime(2024, 3, 9, 0, 0, 0, DateTimeKind.Utc);

        var service = CreateService([
            ("foo", RegularFile, date, []),
        ]);

        var root = service.ListDirectory("/");
        var foo = root!.Single();

        Assert.Equal(date, foo.DateModified);
    }

    [Theory]
    [InlineData(SizeFormat.Bytes, "39000")]
    [InlineData(SizeFormat.Binary, "38 KiB")]
    [InlineData(SizeFormat.SI, "39 KB")]
    public void ListDirectory_FilesIncludeFormattedSize(SizeFormat format, string expected)
    {
        const int Size = 39000;

        var service = CreateService([
            ("foo", RegularFile, DateTime.Now, Enumerable.Repeat<byte>(39, Size).ToArray()),
        ], DefaultOptions with { SizeFormat = format });

        var root = service.ListDirectory("/");
        var foo = root!.Single();

        Assert.Equal(expected, foo.FormattedSize);
    }

    [Fact]
    public void ListDirectory_DirectoriesLeaveMetadataNull()
    {
        var service = CreateService([
            ("foo", Directory, DateTime.Now, []),
            ("bar/file", RegularFile, DateTime.Now, [39])
        ]);

        var root = service.ListDirectory("/");

        Assert.All(root!, x =>
        {
            Assert.Null(x.DateModified);
            Assert.Null(x.FormattedSize);
        });
    }

    [Fact]
    public void ListDirectory_SortsDirectoriesFirst()
    {
        var service = CreateService([
            ("a", Directory, DateTime.Now, []),
            ("b", RegularFile, DateTime.Now, []),
            ("c", Directory, DateTime.Now, []),
            ("d", RegularFile, DateTime.Now, [])
        ]);

        Assert.Equal(["a/", "c/", "b", "d"],
            service.ListDirectory("/")!.Select(x => x.Name));
    }

    [Fact]
    public void ListDirectory_CanSortDirectoriesAlongsideFiles()
    {
        var service = CreateService([
            ("a", Directory, DateTime.Now, []),
            ("b", RegularFile, DateTime.Now, []),
            ("c", Directory, DateTime.Now, []),
            ("d", RegularFile, DateTime.Now, [])
        ], DefaultOptions with
        {
            SortDirectoriesFirst = false
        });

        Assert.Equal(["a/", "b", "c/", "d"],
            service.ListDirectory("/")!.Select(x => x.Name));
    }

    [Fact]
    public void ListDirectory_SortsNumerically() // aka "version" or "natural" sort
    {
        var service = CreateService([
            ("foo10.txt", RegularFile, DateTime.Now, []),
            ("bar", RegularFile, DateTime.Now, []),
            ("foo9.txt", RegularFile, DateTime.Now, []),
        ]);

        Assert.Equal(["bar", "foo9.txt", "foo10.txt"],
            service.ListDirectory("/")!.Select(x => x.Name));
    }

    [Fact]
    public void ListDirectory_IgnoresLeadingSlashOrDotSlash()
    {
        var service = CreateService([
            ("dir 1/dir 2/file 1", RegularFile, DateTime.Now, []),
            ("./dir 1/dir 2/file 2", RegularFile, DateTime.Now, []),
            ("/file 3", RegularFile, DateTime.Now, []),
        ]);

        var rootByEmptyString = service.ListDirectory("");
        var rootBySlash = service.ListDirectory("/");
        var rootByDotSlash = service.ListDirectory("./");
        var rootByDot = service.ListDirectory(".");

        Assert.NotNull(rootByEmptyString);
        Assert.NotNull(rootBySlash);
        Assert.NotNull(rootByDotSlash);
        Assert.NotNull(rootByDot);

        Assert.Equal(rootByEmptyString, rootBySlash);
        Assert.Equal(rootByEmptyString, rootByDotSlash);
        Assert.Equal(rootByEmptyString, rootByDot);

        Assert.Collection(rootBySlash,
            x => Assert.Equal(("dir 1/", "/dir 1/"), (x.Name, x.Path)),
            x => Assert.Equal(("file 3", "/file 3"), (x.Name, x.Path)));

        var dir2 = service.ListDirectory("dir 1/dir 2/");

        Assert.NotNull(dir2);
        Assert.Collection(dir2,
            x => Assert.Equal(("file 1", "/dir 1/dir 2/file 1"), (x.Name, x.Path)),
            x => Assert.Equal(("file 2", "/dir 1/dir 2/file 2"), (x.Name, x.Path)));
    }

    [Fact]
    public void ListDirectory_SupportsSpecialCharacters()
    {
        var service = CreateService([
            ("foo's bar", Directory, DateTime.Now, []),
            ("foo's bar/テスト ñó. 1", Directory, DateTime.Now, []),
            ("foo's bar/テスト ñó. 1/😝", RegularFile, DateTime.Now, [])
        ]);

        var root = service.ListDirectory("/");
        Assert.NotNull(root);
        Assert.Equal(["foo's bar/"], root.Select(x => x.Name));

        var foo = service.ListDirectory("foo's bar");
        Assert.NotNull(foo);
        Assert.Equal(["テスト ñó. 1/"], foo.Select(x => x.Name));

        var test = service.ListDirectory("foo's bar/テスト ñó. 1");
        Assert.NotNull(test);
        Assert.Equal(["😝"], test.Select(x => x.Name));
    }

    [Fact]
    public void GetStream_ReturnsBlobStreamForFile()
    {
        var expected = "リンちゃんマジ天使";

        var service = CreateService([
            ("foo/bar.txt", RegularFile, DateTime.Now, Encoding.UTF8.GetBytes(expected))
        ]);

        using var stream = service.GetStream("foo/bar.txt");

        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        var actual = reader.ReadToEnd();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetStream_ReturnsNullIfNotFound()
    {
        var service = CreateService([]);
        var result = service.GetStream("foo/bar.txt");

        Assert.Null(result);
    }

    [Fact]
    public void GetStream_ReturnsNullIfNotAFile()
    {
        var service = CreateService([
            ("foo", Directory, DateTime.Now, [])
        ]);

        var result = service.GetStream("foo");

        Assert.Null(result);
    }

    [Fact]
    public void GetStream_IgnoresLeadingSlashOrDotSlash()
    {
        var service = CreateService([
            ("foo/test 1", RegularFile, DateTime.Now, Encoding.UTF8.GetBytes("鏡音リン")),
            ("/foo/test 2", RegularFile, DateTime.Now, Encoding.UTF8.GetBytes("初音ミク")),
            ("./foo/test 3", RegularFile, DateTime.Now, Encoding.UTF8.GetBytes("巡音ルカ"))
        ]);

        void GetAndAssert(string path, string expected)
        {
            using var stream = service.GetStream(path);
            Assert.NotNull(stream);
            using var reader = new StreamReader(stream);
            Assert.Equal(expected, reader.ReadToEnd());
        }

        GetAndAssert("/foo/test 1/", "鏡音リン");
        GetAndAssert("./foo/test 2", "初音ミク");
        GetAndAssert("foo/test 3", "巡音ルカ");
    }

    [Fact]
    public void GetStream_SupportsSpecialCharacters()
    {
        var service = CreateService([
            ("foo's bar/テスト ñó. 1/😝", RegularFile, DateTime.Now, [39])
        ]);

        using var stream = service.GetStream("foo's bar/テスト ñó. 1/😝");
        Assert.NotNull(stream);
        Assert.Equal(39, stream.ReadByte());
    }

    [Fact]
    public void GetStream_UncompressesData()
    {
        var service = CreateService([]);

        // Add a row with compressed data, as created by the sqlite3 CLI
        var insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO sqlar(name, mode, mtime, sz, data)
            VALUES ('rin.txt', 33279, 1715759479, 20, unhex('789C2BCAC400790056EF0843'));
            """;
        Assert.Equal(1, insert.ExecuteNonQuery());

        using var stream = service.GetStream("rin.txt");
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        Assert.Equal("riiiiiiiiiiiiiiiiiin", reader.ReadToEnd());
    }

    [Theory]
    [InlineData("foo/bar", true, "/foo/bar/")]
    [InlineData("foo/bar", false, "/foo/bar")]
    [InlineData("/foo/bar", true, "/foo/bar/")]
    [InlineData("/foo/bar/", false, "/foo/bar")]
    [InlineData("./foo/bar", false, "/foo/bar")]
    [InlineData("/", true, "/")]
    [InlineData("./", true, "/")]
    [InlineData(".", true, "/")]
    [InlineData("", true, "/")]
    public void NormalizePath(string input, bool isDirectory, string expected)
    {
        var service = CreateService([]);
        var actual = service.NormalizePath(input, isDirectory);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CanOverrideTableName()
    {
        var service = CreateService([
            ("blah", RegularFile, DateTime.Now, [])
        ], DefaultOptions with
        {
            TableName = "Files"
        });

        var root = service.ListDirectory("/");

        Assert.NotNull(root);
        Assert.Equal(["blah"], root.Select(x => x.Name));
    }

    [Fact]
    public void ResolveSymlink_HandlesSimpleFileSymlinks()
    {
        var service = CreateService([
            ("a/b/relative", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("../thing1")),
            ("a/b/absolute", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("/X/thing2")),
        ]);

        // This is the case where only the last segment is a symlink
        Assert.Equal("/a/thing1", service.ResolveSymlink("/a/b/relative"));
        Assert.Equal("/X/thing2", service.ResolveSymlink("/a/b/absolute"));
    }

    [Fact]
    public void ResolveSymlink_HandlesSimpleDirectorySymlinks()
    {
        var service = CreateService([
            ("a/b/relative", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("../thing1")),
            ("a/b/absolute", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("/X/thing2")),
        ]);

        // This tests that it correctly rewrites the part of the path that's linked to another dir
        Assert.Equal("/a/thing1/c/d", service.ResolveSymlink("/a/b/relative/c/d"));
        Assert.Equal("/X/thing2/c/d", service.ResolveSymlink("/a/b/absolute/c/d"));
    }

    [Fact]
    public void ResolveSymlink_HandlesComplicatedSymlinks()
    {
        var service = CreateService([
            ("a/b", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("B2")),
            ("a/B2", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("/A2")),
            ("A2", Directory, DateTime.Now, Encoding.UTF8.GetBytes("decoy!")),
            ("A2/c", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("./c2/c3/"))
        ]);

        // (╯°□°)╯︵ ┻━┻
        Assert.Equal("/A2/c2/c3/d", service.ResolveSymlink("/a/b/c/d"));
    }

    [Fact]
    public void ResolveSymlink_ThrowsIfRecursive()
    {
        var service = CreateService([
            ("a/b", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("/X")),
            ("X/c", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("/a/b/Y")),
            ("X/Y/d", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("/a/b/Z")),
            ("X/Z", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("Y/d"))
        ]);

        // Segments repeat but not recursive: /a/b/c -> /X/c -> /a/b/Y -> /X/Y
        Assert.Equal("/X/Y", service.ResolveSymlink("/a/b/c"));

        // Recursive: /a/b/c/d -> /X/c/d -> /a/b/Y/d -> ( /X/Y/d -> /a/b/Z -> /X/Z -> /X/Y/d )
        Assert.Throws<RecursiveSymlinkException>(
            () => service.ResolveSymlink("/a/b/c/d"));
    }

    [Fact]
    public void ResolveSymlink_ThrowsIfSelfReferential()
    {
        var service = CreateService([
            ("foo/self", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("self")),
            ("foo/parent", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("."))
        ]);

        // Not self-referencing: /foo/parent/parent -> /foo/parent -> /foo
        Assert.Equal("/foo", service.ResolveSymlink("/foo/parent/parent"));

        // Self-referencing: /foo/self -> /foo/self -> /foo/self -> ...
        Assert.Throws<RecursiveSymlinkException>(
            () => service.ResolveSymlink("/foo/self"));
    }
}
