namespace CalculateFileHashValues.Models;

public sealed record FileHash(string Path, string Hash)
{
    public string Path { get; } = Path;
    public string Hash { get; } = Hash;
}
