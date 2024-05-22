namespace SqliteArchive.Server.Models;

public record IndexModel(string Path, int Count, IReadOnlyList<DirectoryEntryModel> Entries);

public record DirectoryEntryModel(
    string Name,
    string Path,
    DateTime? DateModified = null,
    string FormattedSize = "",
    Mode? Mode = null);
