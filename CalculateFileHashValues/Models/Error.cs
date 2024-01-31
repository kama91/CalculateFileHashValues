namespace CalculateFileHashValues.Models;

public record Error(string Description) : IEntity
{
    public int Id { get; set; }
}