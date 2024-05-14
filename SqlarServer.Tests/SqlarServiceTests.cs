// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using SqlarServer.Models;
using SqlarServer.Services;
using Xunit;

namespace SqlarServer.Tests;

public sealed class SqlarServiceTests : IDisposable
{
    private static readonly SqlarOptions DefaultOptions = new()
    {
        ArchivePath = "",
        TableName = "sqlar",
        NameColumn = "name",
        ModeColumn = "mode",
        DateModifiedColumn = "mtime",
        SizeColumn = "sz",
        DataColumn = "data",
        SizeFormat = SizeFormat.Binary
    };

    // File modes. Can be shown with `stat --format=%f`. Only the S_IFREG & S_IFDIR bits are actually relevant here:
    // https://github.com/torvalds/linux/blob/master/include/uapi/linux/stat.h
    private const int Directory = 0x41ff;
    private const int RegularFile = 0x81a4;

    private readonly SqliteConnection connection;

    public SqlarServiceTests()
    {
        connection = new("Data Source=:memory:");
        connection.Open();
    }

    private SqlarService CreateService(
        IEnumerable<(string Name, int Mode, DateTime DateModified, byte[] Data)> rows,
        SqlarOptions? options = null)
    {
        options ??= DefaultOptions;

        // Create table
        using var createTable = connection.CreateCommand();
        createTable.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {options.TableName} (
              {options.NameColumn} TEXT PRIMARY KEY,  -- name of the file
              {options.ModeColumn} INT,               -- access permissions
              {options.DateModifiedColumn} INT,       -- last modification time
              {options.SizeColumn} INT,               -- original file size
              {options.DataColumn} BLOB               -- compressed content
            )
            """;
        createTable.ExecuteNonQuery();

        // Insert rows
        foreach (var (name, mode, mtime, data) in rows)
        {
            using var insert = connection.CreateCommand();
            insert.CommandText = $"""
                INSERT INTO {options.TableName}({options.NameColumn},{options.ModeColumn},{options.DateModifiedColumn},{options.SizeColumn},{options.DataColumn})
                VALUES($name, $mode, $mtime, $sz, zeroblob($sz));
                SELECT last_insert_rowid();
                """;
            insert.Parameters.AddWithValue("$name", name);
            insert.Parameters.AddWithValue("$mode", mode);
            insert.Parameters.AddWithValue("$mtime", mtime.ToUniversalTime().ToString("s"));
            insert.Parameters.AddWithValue("$sz", data.Length);
            var rowId = (long)insert.ExecuteScalar()!;

            if (data.Length > 0)
            {
                using var blob = new SqliteBlob(connection, options.TableName, options.DataColumn, rowId);
                blob.Write(data);
            }
        }

        // Instantiate service
        return new SqlarService(connection, Options.Create(options));
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
            x => Assert.Equal(("dir 1", "/dir 1/"), (x.Name, x.Path)),
            x => Assert.Equal(("dir 2", "/dir 2/"), (x.Name, x.Path)),
            x => Assert.Equal(("file 1", "/file 1"), (x.Name, x.Path)));

        var dir1 = service.ListDirectory("/dir 1");

        Assert.NotNull(dir1);
        Assert.Collection(dir1,
            x => Assert.Equal(("child dir", "/dir 1/child dir/"), (x.Name, x.Path)),
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
            x => Assert.Equal(("dir 1", "/dir 1/"), (x.Name, x.Path)),
            x => Assert.Equal(("file 1", "/file 1"), (x.Name, x.Path)));

        var dir1 = service.ListDirectory("/dir 1");

        Assert.NotNull(dir1);
        Assert.Collection(dir1,
            x => Assert.Equal(("child dir", "/dir 1/child dir/"), (x.Name, x.Path)),
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
}
