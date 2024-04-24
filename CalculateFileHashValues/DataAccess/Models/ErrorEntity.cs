namespace CalculateFileHashValues.DataAccess.Models;

public sealed record ErrorEntity(string Description) : IEntity
{
    public int Id { get; set; }
}