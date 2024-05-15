// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using SqlarServer.Models;
using SqlarServer.Services;

namespace SqlarServer.Controllers;
public class SqlarController : Controller
{
    private readonly ISqlarService sqlarService;
    private readonly IContentTypeProvider contentTypeProvider;

    public SqlarController(ISqlarService sqlarService, IContentTypeProvider contentTypeProvider)
    {
        this.sqlarService = sqlarService;
        this.contentTypeProvider = contentTypeProvider;
    }

    [Route("{**path}", Name = "Index")]
    public IActionResult Index(string path = "/")
    {
        // Check if requesting a file and return the blob stream if so
        var stream = sqlarService.GetStream(path);
        if (stream is not null)
        {
            if (!contentTypeProvider.TryGetContentType(path, out string? contentType))
            {
                contentType = "application/octet-stream";
            }

            return new FileStreamResult(stream, contentType);
        }

        // See if it's a directory
        var entries = sqlarService.ListDirectory(path);
        if (entries is not null)
        {
            var list = entries.ToList();
            int count = list.Count;
            path = sqlarService.NormalizePath(path, isDirectory: true);

            // Add ".." link
            if (path != "/")
            {
                string parentDirectory = path[..(path.LastIndexOf('/', path.Length - 2) + 1)];
                list.Insert(0, new("../", parentDirectory));
            }

            var model = new IndexModel(path, count, list);
            return View(model);
        }

        return NotFound();
    }
}
