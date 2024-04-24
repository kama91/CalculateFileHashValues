using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace CalculateFileHashValues.HashAlgorithms;

public static class HashAlgorithms
{
    public static async Task<byte[]> ComputeSha256(string path)
    {
        await using var stream = File.OpenRead(path);

        return await SHA256.Create().ComputeHashAsync(stream);
    }
    
    public static byte[] ComputeSha256Sync(string path)
    {
        using var stream = File.OpenRead(path);

        return SHA256.Create().ComputeHash(stream);
    }
    
    public static async Task<byte[]> ComputeMd5(string path)
    {
        await using var stream = File.OpenRead(path);

        return await MD5.Create().ComputeHashAsync(stream);
    }
}