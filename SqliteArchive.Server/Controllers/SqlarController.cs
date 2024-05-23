// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using SqliteArchive.Helpers;
using SqliteArchive.Nodes;
using SqliteArchive.Server.Models;

namespace SqliteArchive.Server.Controllers;
public class SqlarController : Controller
{
    private readonly ISqlarService sqlarService;
    private readonly IContentTypeProvider contentTypeProvider;
    private readonly SqlarOptions options;
    private readonly ILogger<SqlarController> logger;
    private readonly NodeComparer comparer;

    public SqlarController(
        ISqlarService sqlarService,
        IContentTypeProvider contentTypeProvider,
        IOptions<SqlarOptions> options,
        ILogger<SqlarController> logger)
    {
        this.sqlarService = sqlarService;
        this.contentTypeProvider = contentTypeProvider;
        this.options = options.Value;
        this.logger = logger;

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

            if (!string.IsNullOrEmpty(options.Charset))
            {
                contentType += "; charset=" + options.Charset;
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
                    FormattedSize: FormatSize(n.Size, padString: true),
                    Tooltip: $"""
                        {n.Path}{(n is SymbolicLinkNode s ? $" → {s.Target}": "")}

                        Original size: {FormatSize(n.Size)} ({n.Size:N0} bytes)
                        Compressed size: {FormatSize(n.CompressedSize)} ({n.CompressedSize:N0} bytes) ({n.CompressionRatio:P0})
                        Last modified: {n.DateModified.ToLocalTime():F}
                        """,
                    Mode: n.Mode))
                .ToList();

            int count = entries.Count;

            // Add ".." link
            if (!path.IsRoot)
            {
                entries.Insert(0, new("../", path.Parent.ToString(true)));
            }

            var model = new IndexModel(
                Path: path.ToString(true),
                Count: count,
                TotalSize: FormatSize(directory.TotalSize),
                CompressedSize: FormatSize(directory.TotalCompressedSize),
                Ratio: directory.TotalCompressionRatio,
                entries);
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

    private string FormatSize(long size, bool padString = false) => options.SizeFormat switch
    {
        SizeFormat.Binary => FileSizeFormatter.FormatBytes(size).PadLeft(padString ? "1.99 GiB".Length : 0),
        SizeFormat.SI => FileSizeFormatter.FormatBytes(size, true).PadLeft(padString ? "1.99 GB".Length : 0),
        _ => size.ToString()
    };
}
