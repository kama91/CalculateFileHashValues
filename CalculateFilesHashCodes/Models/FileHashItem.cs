namespace CalculateFilesHashCodes.Models
{
    public record FileHashItem : IEntity
    {
        public FileHashItem(string path, string hashValue)
        {
            Path = path;
            HashValue = hashValue;
        }

        public int Id { get; set; }

        public string Path { get; set; }

        public string HashValue { get; set; }
    }
}
