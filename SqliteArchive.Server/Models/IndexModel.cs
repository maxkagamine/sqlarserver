namespace SqliteArchive.Server.Models;

public record IndexModel(string Path, int Count, TimeSpan ExecutionTime, IReadOnlyList<DirectoryEntry> Entries);
