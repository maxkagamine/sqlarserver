namespace SqlarServer.Models;

public record IndexModel(string Path, int Count, IReadOnlyList<DirectoryEntry> Entries);
