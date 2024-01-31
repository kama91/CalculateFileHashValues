using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace CalculateFileHashValues.HashAlgorithms;

public static class HashAlgorithms
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<byte[]> ComputeSha256(string path)
    {
        await using var stream = File.OpenRead(path);

        return await SHA256.Create().ComputeHashAsync(stream);
    }

    public static async Task<byte[]> ComputeMd5(string path)
    {
        await using var stream = File.OpenRead(path);

        return await MD5.Create().ComputeHashAsync(stream);
    }
}