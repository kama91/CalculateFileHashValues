using System;

namespace CalculateFileHashValues.Models;

public readonly struct FileHashItem
{
    public ReadOnlyMemory<char> Path { get; init; }
    public byte[] Hash { get; init; }
}