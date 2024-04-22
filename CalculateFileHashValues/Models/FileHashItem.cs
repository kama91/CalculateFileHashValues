using Microsoft.Extensions.ObjectPool;

namespace CalculateFileHashValues.Models;

public sealed record FileHashItem : IResettable
{
    public string Path { get; set; }
    
    public string HashValue { get; set; }
    
    public bool TryReset()
    {
        Path = string.Empty;
        HashValue = string.Empty;

        return true;
    }
}