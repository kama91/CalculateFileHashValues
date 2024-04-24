using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFileHashValues.DataAccess.Models;
using CalculateFileHashValues.Extensions;
using CalculateFileHashValues.Models;
using CalculateFileHashValues.Services;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.DataAccess;

public sealed class DbService(
    IDataReader<FileHashItem> dataTransformer,
    ErrorService errorService,
    HashValuesContext dataContext,
    HashValuesContext errorContext)
{
    private int _filesCounter;
    
    private readonly HashValuesContext _dataContext =
        dataContext ?? throw new ArgumentNullException(nameof(dataContext));

    private readonly IDataReader<FileHashItem> _dataTransformer =
        dataTransformer ?? throw new ArgumentNullException(nameof(dataTransformer));

    private readonly HashValuesContext _errorContext =
        errorContext ?? throw new ArgumentNullException(nameof(errorContext));

    private readonly ErrorService _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));

    public async Task WriteDataAndErrors()
    {
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
            // TODO change batchSize by settings
            const int batchSize = 1000;
            var batchCount = 0;
            
            await foreach (var item in _dataTransformer.Reader.ReadAllAsync())
            {
                _dataContext.FileHashes.Add(new FileHashEntity(item.Path.ToString(), BitConverter.ToString(item.HashValue.ToArray())));
                ++_filesCounter;
                
                ++batchCount;
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
            _errorService.Writer.TryWrite(new ErrorItem
            {
                Description = ex.ToString()
            });
        }
    }

    private async Task WriteErrorToDb()
    {
        await _errorService.Reader.ProcessData(WriteError);
    }

    private async Task WriteError()
    {
        await foreach (var error in _errorService.Reader.ReadAllAsync())
        {
            await _errorContext.Errors.AddAsync(new ErrorEntity(error.Description));
            await _errorContext.SaveChangesAsync();
        }
    }
}