namespace SqlarServer.Models;

public class SqlarOptions
{
    public required string ArchivePath { get; set; }

    public required string TableName { get; set; }

    public required string NameColumn { get; set; }

    public required string DataColumns { get; set; }
}
