// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using Microsoft.AspNetCore.Mvc;
using SqlarServer.Models;

namespace SqlarServer.Controllers;
public class SqlarController : Controller
{
    [Route("{**path}", Name = "Index")]
    public IActionResult Index(string path = "/")
    {
        path = NormalizePath(path, true);

        var items = new List<ItemModel>()
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

    /// <summary>
    /// Ensures paths start with a leading slash and that directories end in a trailing slash.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <param name="isDirectory">Whether this path is of a directory.</param>
    private static string NormalizePath(string path, bool isDirectory)
    {
        var str = "/" + path.Trim('/');

        if (isDirectory && str != "/")
        {
            str += "/";
        }

        return str;
    }
}
