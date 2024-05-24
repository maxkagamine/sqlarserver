// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using FubarDev.FtpServer;
using FubarDev.FtpServer.FileSystem;
using Microsoft.Extensions.Options;

namespace SqliteArchive.Ftp;

public class SqlarFileSystemProvider(ISqlarService sqlarService, IOptions<SqlarOptions> options) : IFileSystemClassFactory
{
    public Task<IUnixFileSystem> Create(IAccountInformation accountInformation)
        => Task.FromResult<IUnixFileSystem>(new SqlarFileSystem(sqlarService, options));
}
