namespace CalculateFileHashValues.Models;

public readonly struct FileHash(string Path, string Hash)
{
    public string Path { get; } = Path;
    public string Hash { get; } = Hash;
}
