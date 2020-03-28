using System.Data.SQLite;
using CalculateFilesHashCodes.Models;
using Microsoft.EntityFrameworkCore;

namespace CalculateFilesHashCodes.Utils
{
    public sealed class HashSumDbContext : DbContext
    {
        public DbSet<FileNode> FileNodes { get; set; }
        public DbSet<ErrorNode> ErrorNodes { get; set; }

        public HashSumDbContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connection = new SQLiteConnection("Data Source=HashDB.db");
            optionsBuilder.UseSqlite(connection);
        }
    }
}
