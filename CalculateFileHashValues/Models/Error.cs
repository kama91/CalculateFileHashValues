namespace CalculateFileHashValues.Models;

public sealed record Error(string Description) : IEntity
{
    public int Id { get; set; }
}