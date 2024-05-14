// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using Microsoft.AspNetCore.Mvc;
using SqlarServer.Models;
using SqlarServer.Services;

namespace SqlarServer.Controllers;
public class SqlarController : Controller
{
    private readonly ISqlarService sqlarService;

    public SqlarController(ISqlarService sqlarService)
    {
        this.sqlarService = sqlarService;
    }

    [Route("{**path}", Name = "Index")]
    public IActionResult Index(string path = "/")
    {
        path = sqlarService.NormalizePath(path, true); // TODO: Make private if the controller ends up not needing it

        var items = new List<DirectoryEntry>()
        {
            new("foo/", path + "foo/"),
            new("bar.txt", path + "bar.txt", DateTime.UtcNow, FileSizeFormatter.FormatBytes(1000000)),
        };

        if (path != "/")
        {
            items.Insert(0, new("../", path[..(path.LastIndexOf('/', path.Length - 2) + 1)]));
        }

        var model = new IndexModel(path, items);

        return View(model);
    }
}
