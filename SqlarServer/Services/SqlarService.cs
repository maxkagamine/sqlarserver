// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using SqlarServer.Models;

namespace SqlarServer.Services;

public class SqlarService : ISqlarService
{
    private readonly SqliteConnection connection;
    private readonly SqlarOptions options;

    public SqlarService(SqliteConnection connection, IOptions<SqlarOptions> options)
    {
        this.options = options.Value;
        this.connection = connection;
    }

    public IEnumerable<DirectoryEntry>? ListDirectory(string path)
    {
        throw new NotImplementedException();
    }

    public Stream? GetStream(string path)
    {
        throw new NotImplementedException();
    }

    public string NormalizePath(string path, bool isDirectory)
    {
        if (path.StartsWith("./"))
        {
            path = path[2..];
        }

        var str = "/" + path.Trim('/');

        if (isDirectory && str != "/")
        {
            str += "/";
        }

        return str;
    }
}
