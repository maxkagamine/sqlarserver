namespace SqlarServer.Models;

record IndexModel(string Path, IReadOnlyList<ItemModel> Items);

record ItemModel(string Name, string Path);