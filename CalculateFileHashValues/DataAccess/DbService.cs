using System;
using System.Collections.Generic;
using System.Data;
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
    NpgsqlConnection dbConnectionHashes,
    NpgsqlConnection dbConnectionErrors)
{
    private const int BatchSize = 5000;
   
    private int _filesCounter;
    
    private readonly IDataReader<FileHash> _dataTransformer =
        dataTransformer ?? throw new ArgumentNullException(nameof(dataTransformer));

    private readonly ErrorService _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
    private readonly NpgsqlConnection _dbConnectionHashes = dbConnectionHashes ?? throw new ArgumentNullException(nameof(dbConnectionHashes));
    private readonly NpgsqlConnection _dbConnectionErrors = dbConnectionErrors ?? throw new ArgumentNullException(nameof(dbConnectionErrors));

    public async Task WriteDataAndErrors()
    {
        await Task.Yield();

        await Task.WhenAll(_dbConnectionHashes.OpenAsync(), _dbConnectionErrors.OpenAsync());
        
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
        await using var batch = _dbConnectionHashes.CreateBatch();
        foreach (var fileHash in fileHashes)
        {
            var command = new NpgsqlBatchCommand("INSERT INTO hashes.hashes (path, hash) VALUES (@path, @hash)");
            command.Parameters.AddWithValue("path", fileHash.Path);
            command.Parameters.AddWithValue("hash", fileHash.Hash);
            batch.BatchCommands.Add(command);
        }

        await batch.ExecuteNonQueryAsync();
    }

    private async Task WriteErrorToDb()
    {
        await _errorService.Reader.ProcessData(WriteError);
    }

    private async Task WriteError()
    {
        await foreach (var error in _errorService.Reader.ReadAllAsync())
        { 
            await using var command = _dbConnectionErrors.CreateCommand();
            command.CommandText = "INSERT INTO hashes.errors (description) VALUES (@description)";
            command.Parameters.AddWithValue("description", error.Description);
            command.CommandType = CommandType.Text;
            await command.ExecuteNonQueryAsync();
        }
    }
}