using FubarDev.FtpServer.FileSystem;
using SqliteArchive.Nodes;

namespace SqliteArchive.Ftp;

internal class SqlarFileEntry : SqlarFileSystemEntry, IUnixFileEntry
{
    public SqlarFileEntry(Node node) : base(node)
    { }

    public long Size => Node.Size;
}
