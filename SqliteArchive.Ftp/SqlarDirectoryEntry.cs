using FubarDev.FtpServer.FileSystem;
using SqliteArchive.Nodes;

namespace SqliteArchive.Ftp;

internal class SqlarDirectoryEntry : SqlarFileSystemEntry, IUnixDirectoryEntry
{
    public SqlarDirectoryEntry(DirectoryNode node) : base(node)
    { }

    public bool IsRoot => Node.Path.IsRoot;

    public bool IsDeletable => false;
}
