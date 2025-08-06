using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using CalculateFileHashValues.Models;
using CalculateFileHashValues.Services;
using CalculateFileHashValues.Services.Interfaces;

using Npgsql;

namespace CalculateFileHashValues.DataAccess;

public sealed class DbService(
    IChannelDataReader<FileHash> channelDataTransformer,
    ErrorService errorService,
    NpgsqlConnection dbConnectionHashes,
    NpgsqlConnection dbConnectionErrors)
{
    private const int BatchSize = 5000;
   
    private int _filesCounter;
    
    private readonly IChannelDataReader<FileHash> _channelDataTransformer =
        channelDataTransformer ?? throw new ArgumentNullException(nameof(channelDataTransformer));

    private readonly ErrorService _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
    private readonly NpgsqlConnection _dbConnectionHashes = dbConnectionHashes ?? throw new ArgumentNullException(nameof(dbConnectionHashes));
    private readonly NpgsqlConnection _dbConnectionErrors = dbConnectionErrors ?? throw new ArgumentNullException(nameof(dbConnectionErrors));

    public async Task Write()
    {
        await Task.Yield();
        
        await Task.WhenAll(_dbConnectionHashes.OpenAsync(), _dbConnectionErrors.OpenAsync());
        
        await Task.WhenAll(WriteHashes(), WriteErrors());
        
        Console.WriteLine($"{_filesCounter} files were processed"); 
    }

    private async Task WriteHashes()
    {
        try
        {
            var batchedFileHashes = new List<FileHash>(BatchSize);
            await foreach (var fileHash in _channelDataTransformer.Reader.ReadAllAsync())
            {
                batchedFileHashes.Add(fileHash);
                
                if (batchedFileHashes.Count < BatchSize) continue;
            
                await ExecuteBatchCopy(batchedFileHashes);
                _filesCounter += batchedFileHashes.Count;
                batchedFileHashes.Clear();
            }
            
            if (batchedFileHashes.Count > 0)
            {
                await ExecuteBatchCopy(batchedFileHashes);
                _filesCounter += batchedFileHashes.Count;
                batchedFileHashes.Clear();
            }
        }
        catch (Exception ex)
        {
            _errorService.Writer.TryWrite(ex.ToString());
        }
        
        _errorService.Writer.TryComplete();
    }
    
    private async Task ExecuteBatchInsert(IEnumerable<FileHash> fileHashes)
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
    
    private async Task ExecuteBatchCopy(IEnumerable<FileHash> fileHashes)
    {
        await using var writer = await _dbConnectionHashes.BeginBinaryImportAsync(
            "COPY hashes.hashes (path, hash) FROM STDIN (FORMAT BINARY)");

        foreach (var fileHash in fileHashes)
        {
            await writer.StartRowAsync();
            await writer.WriteAsync(fileHash.Path, NpgsqlTypes.NpgsqlDbType.Text);
            await writer.WriteAsync(fileHash.Hash, NpgsqlTypes.NpgsqlDbType.Text);
        }

        await writer.CompleteAsync();
    }


    private async Task WriteErrors()
    {
        await foreach (var error in _errorService.Reader.ReadAllAsync())
        { 
            await using var command = _dbConnectionErrors.CreateCommand();
            command.CommandText = "INSERT INTO hashes.errors (description) VALUES (@description)";
            command.Parameters.AddWithValue("description", error);
            command.CommandType = CommandType.Text;
            await command.ExecuteNonQueryAsync();
        }
    }
}