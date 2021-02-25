using System.IO;
using System.Security.Cryptography;
using CalculateFilesHashCodes.HashCodeAlgorithm.Interfaces;

namespace CalculateFilesHashCodes.HashCodeAlgorithm
{
    public class Md5HashCodeAlgorithm : IHashCodeAlgorithm
    {
        public byte[] ComputeHash(Stream stream)
        {
            return MD5.Create().ComputeHash(stream);
        }
    }
}
