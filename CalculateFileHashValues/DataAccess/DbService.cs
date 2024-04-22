using System;
using System.Threading.Tasks;
using CalculateFileHashValues.DataAccess.Models;
using CalculateFileHashValues.Models;
using CalculateFileHashValues.Services;
using CalculateFileHashValues.Services.Interfaces;
using Microsoft.Extensions.ObjectPool;

namespace CalculateFileHashValues.DataAccess;

public sealed class DbService(
    IDataReader<Task<FileHashItem>> dataTransformer,
    ErrorService errorService,
    HashValuesContext dataContext,
    HashValuesContext errorContext,
    ObjectPool<FileHashItem> hashItemsPool)
{
    private readonly HashValuesContext _dataContext =
        dataContext ?? throw new ArgumentNullException(nameof(dataContext));

    private readonly IDataReader<Task<FileHashItem>> _dataTransformer =
        dataTransformer ?? throw new ArgumentNullException(nameof(dataTransformer));

    private readonly HashValuesContext _errorContext =
        errorContext ?? throw new ArgumentNullException(nameof(errorContext));

    private readonly ObjectPool<FileHashItem> _hashItemsPool = hashItemsPool ?? throw new ArgumentNullException(nameof(hashItemsPool));

    private readonly ErrorService _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));

    public async Task WriteDataAndErrors()
    {
        Console.WriteLine("DbService was started");

        await Task.WhenAll(WriteDataToDb(), WriteErrorToDb());

        Console.WriteLine("DbService has finished work");
    }

    private async Task WriteDataToDb()
    {
        while (!_dataTransformer.Reader.Completion.IsCompleted)
        {
            await WriteData();
        }

        await WriteData();

        _errorService.Writer.Complete();
    }

    private async Task WriteData()
    {
        try
        {
            // TODO change batchSize by settings
            const int batchSize = 1000;
            var batchCount = 0;
            
            while (_dataTransformer.Reader.TryRead(out var item))
            {
                var hashItem = await item;
                _dataContext.FileHashes.Add(new FileHashEntity(hashItem.Path, hashItem.HashValue));
                _hashItemsPool.Return(hashItem);
            
                batchCount++;
            
                if (batchCount < batchSize) continue;
                await _dataContext.SaveChangesAsync();
                batchCount = 0;
            }
            
            if (batchCount > 0)
            {
                await _dataContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _errorService.Writer.TryWrite(new ErrorItem(ex.ToString()));
        }
    }

    private async Task WriteErrorToDb()
    {
        while (!_errorService.Reader.Completion.IsCompleted)
        {
            await WriteError();
        }

        await WriteError();
    }

    private async Task WriteError()
    {
        while (_errorService.Reader.TryRead(out var error))
        {
            await _errorContext.Errors.AddAsync(new ErrorEntity(error.Description));
            await _errorContext.SaveChangesAsync();
        }
    }
}