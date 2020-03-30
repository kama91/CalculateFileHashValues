namespace CalculateFilesHashCodes.Models
{
    public class FileNode
    {
        //[Key]
        public string FilePath { get; set; }
        //[Required]
        public string HashValue { get; set; }

        public FileNode(string filePath, string hashValue)
        {
            FilePath = filePath;
            HashValue = hashValue;
        }
    }
}
