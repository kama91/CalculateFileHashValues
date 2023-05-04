namespace CalculateFilesHashCodes.Models
{
    public record Error : IEntity
    {
        public Error(string description)
        {
            Description = description;
        }

        public int Id { get; set; }

        public string Description { get; set; }
    }
}
