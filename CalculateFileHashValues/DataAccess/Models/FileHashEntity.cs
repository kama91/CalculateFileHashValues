namespace CalculateFileHashValues.DataAccess.Models;

public sealed record FileHashEntity(string Path, string HashValue) : IEntity
{
    public int Id { get; set; }
}   