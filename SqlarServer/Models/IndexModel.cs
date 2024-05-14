namespace SqlarServer.Models;

public record IndexModel(string Path, IReadOnlyList<DirectoryEntry> Entries);
