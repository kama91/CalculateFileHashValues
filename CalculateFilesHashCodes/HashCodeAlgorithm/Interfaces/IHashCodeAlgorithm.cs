using System.IO;

namespace CalculateFilesHashCodes.HashCodeAlgorithm.Interfaces
{
    public interface IHashCodeAlgorithm
    {
        byte[] ComputeHash(string path);
    }
}
