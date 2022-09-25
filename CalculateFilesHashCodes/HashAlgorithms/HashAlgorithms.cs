using System.IO;
using System.Security.Cryptography;

namespace CalculateFilesHashCodes.HashAlgorithms
{
    public static class HashAlgorithms
    {
        public static byte[] ComputeSHA256(string path)
        {
            using var stream = File.OpenRead(path);

            return SHA256.Create().ComputeHash(stream);
        }
    }
}
