using System.Data.SQLite;
using CalculateFilesHashCodes.Models;
using Microsoft.EntityFrameworkCore;

namespace CalculateFilesHashCodes.Common
{
    public sealed class HashCodeDbContext : DbContext
    {
        public DbSet<FileNode> FileNodes { get; set; }
        public DbSet<ErrorNode> ErrorNodes { get; set; }

        public HashCodeDbContext()
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
