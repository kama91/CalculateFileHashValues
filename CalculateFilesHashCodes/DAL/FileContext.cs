using CalculateFilesHashCodes.Models;
using Microsoft.EntityFrameworkCore;

namespace CalculateFilesHashCodes.DAL
{
    public class FileContext : DbContext
    {
        private readonly string _connectionString;

        public FileContext(string connectionString)
        {
            _connectionString = connectionString ?? throw new System.ArgumentNullException(nameof(connectionString));
            Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(@$"Data Source={_connectionString}");
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<FileHashItem> HashItems { get; set; }

        public DbSet<Error> Errors { get; set; }
    }
}
