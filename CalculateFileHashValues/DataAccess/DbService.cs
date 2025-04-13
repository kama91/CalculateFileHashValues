using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculateFileHashValues.DataAccess.Models;
using CalculateFileHashValues.Extensions;
using CalculateFileHashValues.Services;
using CalculateFileHashValues.Services.Interfaces;
using Npgsql;

namespace CalculateFileHashValues.DataAccess;

public sealed class DbService(
    IDataReader<FileHash> dataTransformer,
    ErrorService errorService,
    PostgresConnection dbConnection)
{
    private const int BatchSize = 1000;
   
    private int _filesCounter;
    
    private readonly List<NpgsqlParameter> _parameters = new(BatchSize * 2);
    
    private readonly IDataReader<FileHash> _dataTransformer =
        dataTransformer ?? throw new ArgumentNullException(nameof(dataTransformer));

    private readonly ErrorService _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
    private readonly PostgresConnection _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));

    public async Task WriteDataAndErrors()
    {
        await Task.Yield();
        
        await _dbConnection.OpenConnection();
        
        await Task.WhenAll(WriteDataToDb(), WriteErrorToDb());

        Console.WriteLine($"{_filesCounter} files were processed"); 
    }

    private async Task WriteDataToDb()
    {
        await _dataTransformer.Reader.ProcessData(WriteData);
        
        _errorService.Writer.TryComplete();
    }

    private async Task WriteData()
    {
        try
        {
            var batchedFileHashes = new List<FileHash>(BatchSize);
            await foreach (var fileHash in _dataTransformer.Reader.ReadAllAsync())
            {
                batchedFileHashes.Add(fileHash);
                
                if (batchedFileHashes.Count < BatchSize) continue;

                await ExecuteBatchInsert(batchedFileHashes);
                _filesCounter += batchedFileHashes.Count;
                batchedFileHashes.Clear();
            }

            if (batchedFileHashes.Count > 0)
            {
                await ExecuteBatchInsert(batchedFileHashes);
                _filesCounter += batchedFileHashes.Count;
                batchedFileHashes.Clear();
            }    
        }
        catch (Exception ex)
        {
            _errorService.Writer.TryWrite(new Error(ex.ToString()));
        }
    }
    
    private async Task ExecuteBatchInsert(List<FileHash> fileHashes)
    {
        var sql = new StringBuilder("INSERT INTO hashes.hashes (path, hash) VALUES ");

        for (var i = 0; i < fileHashes.Count; ++i)
        {
            if (i > 0)
            {
                sql.Append(", ");
            }

            sql.Append($"(@path{i}, @hash{i})");
            _parameters.Add(new NpgsqlParameter($"@path{i}", fileHashes[i].Path));
            _parameters.Add(new NpgsqlParameter($"@hash{i}", fileHashes[i].Hash));
        }

        await _dbConnection.ExecuteCommand(sql.ToString(), _parameters);
        
        _parameters.Clear();
    }

    private async Task WriteErrorToDb()
    {
        await _errorService.Reader.ProcessData(WriteError);
    }

    private async Task WriteError()
    {
        await foreach (var error in _errorService.Reader.ReadAllAsync())
        {
           await _dbConnection.ExecuteCommand("INSERT INTO hashes.errors (description) VALUES (@description)", [new NpgsqlParameter("@description", error.Description)]);
        }
    }
}