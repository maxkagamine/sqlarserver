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
    private readonly ILogger<SqlarController> logger;
    private readonly NodeComparer comparer;

    public SqlarController(
        ISqlarService sqlarService,
        IContentTypeProvider contentTypeProvider,
        IOptions<ServerOptions> options,
        ILogger<SqlarController> logger)
    {
        this.sqlarService = sqlarService;
        this.contentTypeProvider = contentTypeProvider;
        this.logger = logger;
        this.options = options.Value;

        comparer = new NodeComparer()
        {
            SortDirectoriesFirst = options.Value.SortDirectoriesFirst
        };
    }

    [HttpGet("{**path=/}", Name = "Index")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index(Path path)
    {
        Node? node = sqlarService.FindPath(path, dereference: true);

        if (node is FileNode file)
        {
            var stream = sqlarService.GetStream(file);

            if (!contentTypeProvider.TryGetContentType(path.ToString(), out string? contentType))
            {
                contentType = "application/octet-stream";
            }

            return File(stream, contentType);
        }

        if (node is DirectoryNode directory)
        {
            var entries = directory.Children
                .Order(comparer)
                .Select(n => new DirectoryEntryModel(
                    Name: n.IsDirectory ? $"{n.Name}/" : n.Name,
                    Path: new Path(path, n.Name).ToString(trailingSlash: n.IsDirectory),
                    DateModified: n.DateModified,
                    FormattedSize: FormatSize(n)))
                .ToList();

            int count = entries.Count;

            // Add ".." link
            if (!path.IsRoot)
            {
                entries.Insert(0, new("../", path.Parent.ToString(true)));
            }

            var model = new IndexModel(path.ToString(true), count, entries);
            return View(model);
        }

        if (node is not null)
        {
            logger.LogInformation("\"{Path}\" is {Type}", path, node is SymbolicLinkNode ?
                "a broken or recursive symlink" :
                "an unsupported type (e.g. pipe, block device)");

            return UnprocessableEntity();
        }

        return NotFound();
    }

    private string? FormatSize(Node node) => (node.IsDirectory, options.SizeFormat) switch
    {
        (true, _) => null,
        (_, SizeFormat.Binary) => FileSizeFormatter.FormatBytes(node.Size),
        (_, SizeFormat.SI) => FileSizeFormatter.FormatBytes(node.Size, true),
        _ => node.Size.ToString()
    };
}
