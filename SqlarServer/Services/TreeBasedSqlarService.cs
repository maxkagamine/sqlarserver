using SqlarServer.Models;

namespace SqlarServer.Services;

public class TreeBasedSqlarService : ISqlarService
{
    public IEnumerable<DirectoryEntry>? ListDirectory(string path)
    {
        throw new NotImplementedException();
    }

    public Stream? GetStream(string path)
    {
        throw new NotImplementedException();
    }

    public string NormalizePath(string path, bool isDirectory)
    {
        throw new NotImplementedException();
    }
}
