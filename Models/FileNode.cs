namespace CalculateFilesHashCodes.Models
{
    public class FileNode
    {
        //[Key]
        public string FileName { get; set; }
        //[Required]
        public string HashValue { get; set; }

        public FileNode(string fileName, string hashValue)
        {
            FileName = fileName;
            HashValue = hashValue;
        }
    }
}
