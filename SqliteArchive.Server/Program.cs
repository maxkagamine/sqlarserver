// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using SqliteArchive;

static void Help()
{
    Console.Error.WriteLine("Usage: docker run -it --rm -v .:/srv:ro -p 3939:80 SqliteArchive.Server path/to/sqlite.db\n");
    Console.Error.WriteLine(SqlarOptions.HelpText);
    Environment.Exit(1);
}

if (args.Length != 1 || args[0] == "--help" || args[0] == "-h")
{
    Help();
}

if (!File.Exists(args[0]))
{
    Console.Error.WriteLine($"\u001b[31m\"{args[0]}\" does not exist.");
    Console.Error.WriteLine($"Working directory is {Environment.CurrentDirectory}; is the volume mounted there?\u001b[m\n");
    Help();
}

var builder = WebApplication.CreateSlimBuilder();

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<SqlarOptions>>().Value;
    var connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = args[0],
        Mode = SqliteOpenMode.ReadOnly
    };

    var connection = new SqliteConnection(connectionString.ToString());
    connection.Open();

    return connection;
});

builder.Services.AddOptions<SqlarOptions>().BindConfiguration("");
builder.Services.AddSingleton<ISqlarService, SqlarService>();

builder.Services.AddSingleton<IContentTypeProvider>(new FileExtensionContentTypeProvider()
{
    Mappings =
    {
        // A few common browser-supported formats not included in .NET's content type map
        { ".avif", "image/avif" },
        { ".flac", "audio/flac" },
        { ".opus", "audio/ogg" },
    }
});

var app = builder.Build();

app.UseDeveloperExceptionPage();

app.UseRouting();
app.MapControllers();

// Initialize file tree at startup
app.Services.GetRequiredService<ISqlarService>();

app.Run();
