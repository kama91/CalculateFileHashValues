using System.IO;
using System.Security.Cryptography;
using CalculateFilesHashCodes.HashCodeAlgorithm.Interfaces;

namespace CalculateFilesHashCodes.HashCodeAlgorithm
{
    public class Sha256HashCodeAlgorithm : IHashCodeAlgorithm
    {
        public byte[] ComputeHash(string path)
        {
            using var stream = File.OpenRead(path);
            
            return SHA256.Create().ComputeHash(stream);
        }
    }
}
