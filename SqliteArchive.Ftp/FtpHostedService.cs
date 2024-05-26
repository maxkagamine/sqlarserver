// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using FubarDev.FtpServer;
using Microsoft.Extensions.Hosting;

namespace SqliteArchive.Ftp;

public class FtpHostedService(IFtpServerHost ftpServerHost) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => ftpServerHost.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => ftpServerHost.StopAsync(cancellationToken);
}
