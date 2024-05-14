namespace SqlarServer.Models;

public record DirectoryEntry(string Name, string Path, DateTime? DateModified = null, string? FormattedSize = null);
