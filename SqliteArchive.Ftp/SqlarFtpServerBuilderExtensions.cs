// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using FubarDev.FtpServer;
using FubarDev.FtpServer.FileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace SqliteArchive.Ftp;

public static class SqlarFtpServerBuilderExtensions
{
    /// <summary>
    /// Uses the Sqlar file system API.
    /// </summary>
    /// <param name="builder">The server builder used to configure the FTP server.</param>
    /// <returns>The server builder used to configure the FTP server.</returns>
    public static IFtpServerBuilder UseSqlarFileSystem(this IFtpServerBuilder builder)
    {
        builder.Services.AddSingleton<IFileSystemClassFactory, SqlarFileSystemProvider>();
        return builder;
    }
}
