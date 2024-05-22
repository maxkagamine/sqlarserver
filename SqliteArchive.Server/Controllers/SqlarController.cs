// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using SqliteArchive.Helpers;
using SqliteArchive.Nodes;
using SqliteArchive.Server.Models;
using System.Collections.Generic;
using System.Diagnostics;

namespace SqliteArchive.Server.Controllers;
public class SqlarController : Controller
{
    private readonly ISqlarService sqlarService;
    private readonly IContentTypeProvider contentTypeProvider;
    private readonly ServerOptions options;
    private readonly NodeComparer comparer;

    public SqlarController(ISqlarService sqlarService, IContentTypeProvider contentTypeProvider, IOptions<ServerOptions> options)
    {
        this.sqlarService = sqlarService;
        this.contentTypeProvider = contentTypeProvider;
        this.options = options.Value;

        comparer = new NodeComparer()
        {
            SortDirectoriesFirst = options.Value.SortDirectoriesFirst
        };
    }

    [HttpGet("{**path}", Name = "Index")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index(string path = "")
    {
        Node? node = sqlarService.FindPath(path);

        if (node is FileNode file)
        {
            var stream = sqlarService.GetStream(file);

            if (!contentTypeProvider.TryGetContentType(path, out string? contentType))
            {
                contentType = "application/octet-stream";
            }

            return File(stream, contentType);
        }

        if (node is DirectoryNode directory)
        {
            path = $"/{path.Trim('/')}/";

            var entries = directory.Children
                .Order(comparer)
                .Select(n => new DirectoryEntryModel(
                    Name: FormatName(n),
                    Path: CreatePath(n, path), // Node.Path is the "realpath", but we want to preserve symlinks
                    DateModified: n.DateModified,
                    FormattedSize: FormatSize(n)))
                .ToList();

            int count = entries.Count;

            // Add ".." link
            if (directory.Parent is not null)
            {
                string parentDirectory = path[..(path.LastIndexOf('/', path.Length - 2) + 1)];
                entries.Insert(0, new("../", parentDirectory));
            }

            var model = new IndexModel(path, count, entries);
            return View(model);
        }

        return NotFound();
    }

    private static string FormatName(Node node) => node.IsDirectory ? $"{node.Name}/" : node.Name;

    private static string CreatePath(Node node, string basePath) => $"{basePath.TrimEnd('/')}/{FormatName(node)}";

    private string? FormatSize(Node node) => (node.IsDirectory, options.SizeFormat) switch
    {
        (true, _) => null,
        (_, SizeFormat.Binary) => FileSizeFormatter.FormatBytes(node.Size),
        (_, SizeFormat.SI) => FileSizeFormatter.FormatBytes(node.Size, true),
        _ => node.Size.ToString()
    };
}
