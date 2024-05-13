// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using Microsoft.AspNetCore.Mvc;
using SqlarServer.Models;

namespace SqlarServer.Controllers;
public class SqlarController : Controller
{
    [Route("{**path}")]
    public IActionResult Index(string path = "/")
    {
        path = NormalizePath(path);

        var items = new List<ItemModel>()
        {
            new("foo", $"{(path == "/" ? "" : path)}/foo"),
            new("bar", $"{(path == "/" ? "" : path)}/bar")
        };

        if (path != "/")
        {
            items.Insert(0, new("..", NormalizePath(path[..path.LastIndexOf('/')])));
        }

        var model = new IndexModel(path, items);

        return View(model);
    }

    private static string NormalizePath(string path)
        => "/" + path.Trim('/');
}
