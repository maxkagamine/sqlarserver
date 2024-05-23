// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SqliteArchive.Nodes;
using Xunit;

namespace SqliteArchive.Tests;

public sealed class SqlarServiceTests : IDisposable
{
    // File modes. Can be shown with `stat --format=%f`. Only the S_IFMT bits are actually relevant here:
    // https://github.com/torvalds/linux/blob/master/include/uapi/linux/stat.h
    private const int Directory = 0x41ff;
    private const int RegularFile = 0x81a4;
    private const int Symlink = 0xa1ff;

    private readonly SqliteConnection connection;

    private SqlarOptions options = new();
    private ILogger<SqlarService> logger = NullLogger<SqlarService>.Instance;

    public SqlarServiceTests()
    {
        connection = new("Data Source=:memory:");
        connection.Open();
    }

    private SqlarService CreateService(
        IEnumerable<(string Name, int Mode, DateTime DateModified, byte[] Data)> rows,
        Action? beforeCreateService = null)
    {
        // Create table
        using var createTable = connection.CreateCommand();
        createTable.CommandText = """
            CREATE TABLE IF NOT EXISTS sqlar (
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
            insert.CommandText = """
                INSERT INTO sqlar(name, mode, mtime, sz, data)
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
                using var blob = new SqliteBlob(connection, "sqlar", "data", rowId);
                blob.Write(data);
            }
        }

        // Instantiate service
        beforeCreateService?.Invoke();
        return new SqlarService(connection, Options.Create(options), logger);
    }

    public void Dispose() => connection.Dispose();

    [Fact]
    public void ListsExplicitDirectories()
    {
        var service = CreateService([
            ("dir 1", Directory, DateTime.Now, []),
            ("dir 1/child dir", Directory, DateTime.Now, []),
            ("dir 1/child file", RegularFile, DateTime.Now, []),
            ("dir 2", Directory, DateTime.Now, []),
            ("file 1", RegularFile, DateTime.Now, []),
        ]);

        var root = service.FindPath("/") as DirectoryNode;

        Assert.NotNull(root);
        Assert.Collection(root.Children,
            x => Assert.Equal(("dir 1", "/dir 1"), (x.Name, x.Path.ToString())),
            x => Assert.Equal(("dir 2", "/dir 2"), (x.Name, x.Path.ToString())),
            x => Assert.Equal(("file 1", "/file 1"), (x.Name, x.Path.ToString())));

        var dir1 = service.FindPath("/dir 1") as DirectoryNode;

        Assert.NotNull(dir1);
        Assert.Collection(dir1.Children,
            x => Assert.Equal(("child dir", "/dir 1/child dir"), (x.Name, x.Path.ToString())),
            x => Assert.Equal(("child file", "/dir 1/child file"), (x.Name, x.Path.ToString())));

        var childDir = service.FindPath("/dir 1/child dir") as DirectoryNode;

        Assert.NotNull(childDir);
        Assert.Empty(childDir.Children);
    }

    [Fact]
    public void ListsImplicitDirectories()
    {
        var service = CreateService([
            ("dir 1/child dir/another file", RegularFile, DateTime.Now, []),
            ("dir 1/child file", RegularFile, DateTime.Now, []),
            ("file 1", RegularFile, DateTime.Now, []),
        ]);

        var root = service.FindPath("/") as DirectoryNode;

        Assert.NotNull(root);
        Assert.Collection(root.Children,
            x => Assert.Equal(("dir 1", "/dir 1"), (x.Name, x.Path.ToString())),
            x => Assert.Equal(("file 1", "/file 1"), (x.Name, x.Path.ToString())));

        var dir1 = service.FindPath("/dir 1") as DirectoryNode;

        Assert.NotNull(dir1);
        Assert.Collection(dir1.Children,
            x => Assert.Equal(("child dir", "/dir 1/child dir"), (x.Name, x.Path.ToString())),
            x => Assert.Equal(("child file", "/dir 1/child file"), (x.Name, x.Path.ToString())));
    }

    [Fact]
    public void FindPathReturnsNullIfNotFound()
    {
        var service = CreateService([
            ("foo", Directory, DateTime.Now, []),
        ]);

        Assert.NotNull(service.FindPath("foo"));
        Assert.Null(service.FindPath("bar"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyArchiveIsEmpty(bool includeRootInArchive)
    {
        var service = CreateService(includeRootInArchive ?
            [(".", Directory, DateTime.Now, [])] : // This can happen if running `sqlite3 -Acf <db> .` in an empty dir
            []);
        var root = service.FindPath("/") as DirectoryNode;

        Assert.NotNull(root);
        Assert.Empty(root.Children);
    }

    [Fact]
    public void NodesIncludeDateModified()
    {
        var date = new DateTime(2024, 3, 9, 0, 0, 0, DateTimeKind.Utc);

        var service = CreateService([
            ("foo", RegularFile, date, []),
        ]);

        var foo = service.FindPath("foo") as FileNode;

        Assert.NotNull(foo);
        Assert.Equal(date, foo.DateModified);
    }

    [Fact]
    public void ImplicitDirectoriesHaveAutomaticDateModified()
    {
        var date1 = new DateTime(2017, 3, 9, 0, 0, 0, DateTimeKind.Utc);
        var date2 = new DateTime(2024, 3, 9, 0, 0, 0, DateTimeKind.Utc);
        var date3 = new DateTime(2050, 3, 9, 0, 0, 0, DateTimeKind.Utc);

        var service = CreateService([
            ("implicit dir/foo", RegularFile, date1, []),
            ("implicit dir/bar", RegularFile, date2, []),
            ("explicit dir/stuff", RegularFile, date3, []),
            ("explicit dir", Directory, date1, []),
            ("empty dir", Directory, date3, []),
        ]);

        var implicitDir = service.FindPath("implicit dir");
        var explicitDir = service.FindPath("explicit dir");
        var emptyDir = service.FindPath("empty dir");

        Assert.Equal(date2, implicitDir?.DateModified);
        Assert.Equal(date1, explicitDir?.DateModified);
        Assert.Equal(date3, emptyDir?.DateModified);
    }

    [Fact]
    public void FilesIncludeSize()
    {
        int size = 39000;

        var service = CreateService([
            ("foo", RegularFile, DateTime.Now, Enumerable.Repeat<byte>(39, size).ToArray()),
        ]);

        var foo = service.FindPath("foo") as FileNode;

        Assert.NotNull(foo);
        Assert.Equal(size, foo.Size);
    }

    [Fact]
    public void CalculatesTotalSizeOfDirectories()
    {
        int expectedTotalSize = 39;
        int expectedTotalCompressedSize = 31;
        double expectedTotalCompressionRatio = 0.2051;

        var service = CreateService([
            ("dir/uncompressed", RegularFile, DateTime.Now, Enumerable.Repeat<byte>(0, 19).ToArray()),
        ], () =>
        {
            var insert = connection.CreateCommand();
            insert.CommandText = """
                INSERT INTO sqlar(name, mode, mtime, sz, data)
                VALUES ('dir/compressed', 33279, 1715759479, 20, unhex('789C2BCAC400790056EF0843'));
                """;
            Assert.Equal(1, insert.ExecuteNonQuery());
        });

        var dir = Assert.IsType<DirectoryNode>(service.FindPath("dir"));

        Assert.Equal(expectedTotalSize, dir.TotalSize);
        Assert.Equal(expectedTotalCompressedSize, dir.TotalCompressedSize);
        Assert.Equal(expectedTotalCompressionRatio, dir.TotalCompressionRatio, tolerance: 0.001);
    }

    [Fact]
    public void IgnoresLeadingSlashOrDotSlash()
    {
        var service = CreateService([
            ("dir 1/dir 2/file 1", RegularFile, DateTime.Now, []),
            ("./dir 1/dir 2/file 2", RegularFile, DateTime.Now, []),
            ("/file 3", RegularFile, DateTime.Now, []),
        ]);

        var root = service.FindPath("/") as DirectoryNode;

        Assert.NotNull(root);
        Assert.Collection(root.Children,
            x => Assert.Equal(("dir 1", "/dir 1"), (x.Name, x.Path.ToString())),
            x => Assert.Equal(("file 3", "/file 3"), (x.Name, x.Path.ToString())));

        var dir2 = service.FindPath("dir 1/dir 2") as DirectoryNode;

        Assert.NotNull(dir2);
        Assert.Collection(dir2.Children,
            x => Assert.Equal(("file 1", "/dir 1/dir 2/file 1"), (x.Name, x.Path.ToString())),
            x => Assert.Equal(("file 2", "/dir 1/dir 2/file 2"), (x.Name, x.Path.ToString())));
    }

    [Fact]
    public void SupportsSpecialCharacters()
    {
        var service = CreateService([
            ("foo's bar", Directory, DateTime.Now, []),
            ("foo's bar/テスト ñó. 1", Directory, DateTime.Now, []),
            ("foo's bar/テスト ñó. 1/😝", RegularFile, DateTime.Now, [])
        ]);

        var root = service.FindPath("/") as DirectoryNode;
        Assert.NotNull(root);
        Assert.Equal(["foo's bar"], root.Children.Select(x => x.Name));

        var foo = service.FindPath("foo's bar") as DirectoryNode;
        Assert.NotNull(foo);
        Assert.Equal(["テスト ñó. 1"], foo.Children.Select(x => x.Name));

        var test = service.FindPath("foo's bar/テスト ñó. 1") as DirectoryNode;
        Assert.NotNull(test);
        Assert.Equal(["😝"], test.Children.Select(x => x.Name));

        var smiley = service.FindPath("foo's bar/テスト ñó. 1/😝") as FileNode;
        Assert.NotNull(smiley);
    }

    [Fact]
    public void GetStreamReturnsBlobStream()
    {
        var expected = "リンちゃんマジ天使";

        var service = CreateService([
            ("foo/bar.txt", RegularFile, DateTime.Now, Encoding.UTF8.GetBytes(expected))
        ]);

        var node = Assert.IsType<FileNode>(service.FindPath("foo/bar.txt"));

        using var stream = service.GetStream(node);
        using var reader = new StreamReader(stream);
        var actual = reader.ReadToEnd();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetStreamUncompressesData()
    {
        var service = CreateService([], beforeCreateService: () =>
        {
            // Manually add a row with compressed data, matching the sqlite3 CLI
            var insert = connection.CreateCommand();
            insert.CommandText = """
                INSERT INTO sqlar(name, mode, mtime, sz, data)
                VALUES ('rin.txt', 33279, 1715759479, 20, unhex('789C2BCAC400790056EF0843'));
                """;
            Assert.Equal(1, insert.ExecuteNonQuery());
        });

        var node = Assert.IsType<FileNode>(service.FindPath("rin.txt"));

        using var stream = service.GetStream(node);
        using var reader = new StreamReader(stream);

        Assert.Equal("riiiiiiiiiiiiiiiiiin", reader.ReadToEnd());
    }

    [Fact]
    public void HandlesSimpleFileSymlinks()
    {
        // This is the case where only the last segment is a symlink
        var service = CreateService([
            ("a/b/relative", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("../thing1")),
            ("a/b/absolute", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("/X/thing2")),
            ("a/thing1", RegularFile, DateTime.Now, []),
            ("X/thing2", RegularFile, DateTime.Now, []),
        ]);

        var relative = Assert.IsType<SymbolicLinkNode>(service.FindPath("/a/b/relative"));
        var absolute = Assert.IsType<SymbolicLinkNode>(service.FindPath("/a/b/absolute"));

        Assert.Equal("/a/thing1", relative.TargetNode?.Path.ToString());
        Assert.Equal("/X/thing2", absolute.TargetNode?.Path.ToString());
    }

    [Fact]
    public void HandlesSimpleDirectorySymlinks()
    {
        // This tests that it correctly rewrites the part of the path that's linked to another dir
        var service = CreateService([
            ("a/b/relative", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("../thing1")),
            ("a/b/absolute", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("/X/thing2")),
            ("a/thing1/c/d", RegularFile, DateTime.Now, []),
            ("X/thing2/c/d", RegularFile, DateTime.Now, []),
        ]);

        var relative = Assert.IsType<FileNode>(service.FindPath("/a/b/relative/c/d"));
        var absolute = Assert.IsType<FileNode>(service.FindPath("/a/b/absolute/c/d"));

        Assert.Equal("/a/thing1/c/d", relative.Path.ToString());
        Assert.Equal("/X/thing2/c/d", absolute.Path.ToString());
    }

    [Fact]
    public void HandlesComplicatedSymlinks()
    {
        // (╯°□°)╯︵ ┻━┻
        var service = CreateService([
            ("a/b", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("B2")),
            ("a/B2", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("/A2")),
            ("A2", Directory, DateTime.Now, Encoding.UTF8.GetBytes("decoy!")),
            ("A2/c", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("./c2/c3/")),
            ("A2/c2/c3/d", RegularFile, DateTime.Now, []),
        ]);

        var symlink = Assert.IsType<FileNode>(service.FindPath("/a/b/c/d"));

        // /a/b/c/d -> /a/B2/c/d -> /A2/c/d -> /A2/c2/c3/d
        Assert.Equal("/A2/c2/c3/d", symlink.Path.ToString());
    }

    [Fact]
    public void SymlinkTargetNodeIsNullIfRecursive()
    {
        var service = CreateService([
            ("a/b", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("/X")),
            ("X/c", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("/a/b/Y")),
            ("X/Y/d", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("/a/b/Z")),
            ("X/Z", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("Y/d"))
        ]);

        // Segments repeat but not recursive: /a/b/c -> /X/c -> /a/b/Y -> /X/Y
        var ok = Assert.IsType<SymbolicLinkNode>(service.FindPath("/a/b/c"));
        Assert.Equal("/X/Y", ok.TargetNode?.Path.ToString());

        // Recursive: /a/b/c/d -> /X/c/d -> /a/b/Y/d -> ( /X/Y/d -> /a/b/Z -> /X/Z -> /X/Y/d )
        var notOk = Assert.IsType<SymbolicLinkNode>(service.FindPath("/a/b/c/d"));
        Assert.Null(notOk.TargetNode);
    }

    [Fact]
    public void SymlinkTargetNodeIsNullIfSelfReferential()
    {
        var service = CreateService([
            ("foo/self", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("self")),
            ("foo/parent", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("."))
        ]);

        // Not self-referencing: /foo/parent/parent -> /foo/parent -> /foo
        var ok = Assert.IsType<SymbolicLinkNode>(service.FindPath("/foo/parent/parent"));
        Assert.Equal("/foo", ok.TargetNode?.Path.ToString());

        // Self-referencing: /foo/self -> /foo/self -> /foo/self -> ...
        var notOk = Assert.IsType<SymbolicLinkNode>(service.FindPath("/foo/self"));
        Assert.Null(notOk.TargetNode);
    }

    [Fact]
    public void DereferencesSymlinks()
    {
        var service = CreateService([
            ("foo", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("bar")),
            ("foo2", Symlink, DateTime.Now, Encoding.UTF8.GetBytes("bar2")),
            ("bar", RegularFile, DateTime.Now, []),
        ]);

        var foo = Assert.IsType<FileNode>(service.FindPath("foo", dereference: true));
        Assert.Equal("/bar", foo.Path.ToString());

        var foo2 = Assert.IsType<SymbolicLinkNode>(service.FindPath("foo2", dereference: true));
        Assert.Null(foo2.TargetNode);
    }

    [Fact]
    public void CaseSensitiveFileSystem()
    {
        var service = CreateService([
            ("dir/foo", RegularFile, DateTime.Now, []),
            ("dir/Foo", RegularFile, DateTime.Now, []),
            ("Dir/bar", RegularFile, DateTime.Now, []),
        ]);

        var root = Assert.IsType<DirectoryNode>(service.FindPath("/"));
        var lowercaseDir = Assert.IsType<DirectoryNode>(service.FindPath("dir"));

        Assert.Equal(["dir", "Dir"], root.Children.Select(n => n.Name));
        Assert.Equal(["foo", "Foo"], lowercaseDir.Children.Select(n => n.Name));
        Assert.Null(service.FindPath("DIR/FOO"));
    }

    [Fact]
    public void CaseInsensitiveFileSystem()
    {
        var mockLogger = new Mock<ILogger<SqlarService>>();
        
        options = options with { CaseInsensitive = true };
        logger = mockLogger.Object;

        var service = CreateService([
            ("dir/foo", RegularFile, DateTime.Now, []),
            ("dir/Foo", RegularFile, DateTime.Now, []),
            ("Dir/bar", RegularFile, DateTime.Now, []),
        ]);

        var root = Assert.IsType<DirectoryNode>(service.FindPath("/"));
        var lowercaseDir = Assert.IsType<DirectoryNode>(service.FindPath("dir"));

        Assert.Equal(["dir"], root.Children.Select(n => n.Name));
        Assert.Equal(["foo", "bar"], lowercaseDir.Children.Select(n => n.Name));
        Assert.IsType<FileNode>(service.FindPath("DIR/FOO"));

        // Should log a warning about the duplicate file entry
        mockLogger.Verify(logger => logger.Log(
            LogLevel.Warning, 0,
            It.Is<It.IsAnyType>((o, t) => o.ToString() == "Path \"/dir/Foo\" exists in the archive multiple times."),
            null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }
}
