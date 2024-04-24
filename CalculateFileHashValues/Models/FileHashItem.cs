using System;

namespace CalculateFileHashValues.Models;

public readonly struct FileHashItem
{
    public ReadOnlyMemory<char> Path { get; init; }
    public ReadOnlyMemory<byte> HashValue { get; init; }
}