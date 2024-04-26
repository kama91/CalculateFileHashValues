namespace CalculateFileHashValues.DataAccess.Models;

public sealed record FileHashEntity(string Path, string Hash) : IEntity
{
    public int Id { get; set; }
}   