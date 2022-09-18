namespace CalculateFilesHashCodes.Models
{
    public record FileHashItem
    {
        public FileHashItem(string path, string hashValue)
        {
            Path = path;
            HashValue = hashValue;
        }

        public string Path { get; }

        public string HashValue { get; }
    }
}
