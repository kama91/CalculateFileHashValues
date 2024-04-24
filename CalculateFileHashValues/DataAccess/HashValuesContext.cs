using System;
using CalculateFileHashValues.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace CalculateFileHashValues.DataAccess;

public sealed class HashValuesContext : DbContext
{
    private readonly string _connectionString;

    public HashValuesContext(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        //Database.EnsureDeleted();
        Database.EnsureCreated();
    }

    public DbSet<FileHashEntity> FileHashes { get; set; }
    public DbSet<ErrorEntity> Errors { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_connectionString, 
            o => o.MinBatchSize(1000));
        base.OnConfiguring(optionsBuilder);
    }
}