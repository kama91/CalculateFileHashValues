namespace CalculateFileHashValues.Models;

public sealed record FileHashItem(string Path, string HashValue) : IEntity
{
    public int Id { get; set; }
}