using System;
using CalculateFileHashValues.Models;
using Microsoft.EntityFrameworkCore;

namespace CalculateFileHashValues.DAL;

public sealed class HashValuesContext : DbContext
{
    private readonly string _connectionString;

    public HashValuesContext(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        Database.EnsureDeleted();
        Database.EnsureCreated();
    }

    public DbSet<FileHashItem> HashItems { get; set; }

    public DbSet<Error> Errors { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(@$"Data Source={_connectionString}");
        base.OnConfiguring(optionsBuilder);
    }
}