using FubarDev.FtpServer.FileSystem;
using SqliteArchive.Nodes;

namespace SqliteArchive.Ftp;

internal class SqlarFileEntry : SqlarFileSystemEntry, IUnixFileEntry
{
    public SqlarFileEntry(Node node, string? name = null) : base(node, name)
    { }

    public long Size => Node.Size;
}
