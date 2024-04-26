using System;
using CalculateFileHashValues.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace CalculateFileHashValues.DataAccess;

public sealed class HashValuesContext : DbContext
{
   private const string Schema = "hash_values";
    
    private readonly string _connectionString;

    public HashValuesContext(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        Database.EnsureCreated();
    }

    public DbSet<FileHashEntity> FileHashes { get; set; }
    public DbSet<ErrorEntity> Errors { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_connectionString);
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileHashEntity>()
            .ToTable("files_hashes", Schema);
        
        modelBuilder.Entity<ErrorEntity>()
            .ToTable("errors", Schema);
    }
}