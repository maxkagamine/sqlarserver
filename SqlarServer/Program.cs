// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using SqlarServer.Models;
using SqlarServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<SqlarOptions>>().Value;
    var connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = options.ArchivePath,
        Mode = SqliteOpenMode.ReadOnly
    };

    var connection = new SqliteConnection(connectionString.ToString());
    connection.Open();

    return connection;
});

builder.Services.AddOptions<SqlarOptions>().BindConfiguration("");
builder.Services.AddSingleton<ISqlarService, SqlarService>();

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
