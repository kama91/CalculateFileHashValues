namespace CalculateFilesHashCodes.Models
{
    public class FileNode
    {
        public string FilePath { get; }
        
        public string HashValue { get; }

        public FileNode(string filePath, string hashValue)
        {
            FilePath = filePath;
            HashValue = hashValue;
        }
    }
}
