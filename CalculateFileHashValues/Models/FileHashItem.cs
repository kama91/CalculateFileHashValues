namespace CalculateFileHashValues.Models;

public record FileHashItem(string Path, string HashValue) : IEntity
{
    public int Id { get; set; }
}