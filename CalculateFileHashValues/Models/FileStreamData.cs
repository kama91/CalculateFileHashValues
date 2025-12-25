using System.IO;

namespace CalculateFileHashValues.Models;

public sealed record FileStreamData(string Path, FileStream DataStream);