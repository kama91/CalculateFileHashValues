using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFileHashValues.Models;
using CalculateFileHashValues.Services;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.DAL;

public sealed class DbService(
    IDataReader<Task<FileHashItem>> dataTransformer,
    ErrorService errorService,
    HashValuesContext dataContext,
    HashValuesContext errorContext)
{
    private readonly HashValuesContext _dataContext =
        dataContext ?? throw new ArgumentNullException(nameof(dataContext));

    private readonly IDataReader<Task<FileHashItem>> _dataTransformer =
        dataTransformer ?? throw new ArgumentNullException(nameof(dataTransformer));

    private readonly HashValuesContext _errorContext =
        errorContext ?? throw new ArgumentNullException(nameof(errorContext));

    private readonly ErrorService _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));

    public async Task WriteDataAndErrors()
    {
        Console.WriteLine("DbService was started");

        var tasks = new List<Task>
        {
            WriteDataToDb(),
            WriteErrorToDb()
        };

        await Task.WhenAll(tasks);

        await _dataContext.SaveChangesAsync();
        await _errorContext.SaveChangesAsync();

        Console.WriteLine("DbService has finished work");
    }

    private async Task WriteDataToDb()
    {
        while (!_dataTransformer.Reader.Completion.IsCompleted) await WriteData();

        await WriteData();

        _errorService.Writer.Complete();
    }

    private async Task WriteData()
    {
        try
        {
            while (_dataTransformer.Reader.TryRead(out var item)) await _dataContext.HashItems.AddAsync(await item);
        }
        catch (Exception ex)
        {
            _errorService.Writer.TryWrite(new Error(ex.ToString()));
        }
    }

    private async Task WriteErrorToDb()
    {
        while (!_errorService.Reader.Completion.IsCompleted) await WriteError();

        await WriteError();
    }

    private async Task WriteError()
    {
        while (_errorService.Reader.TryRead(out var error)) await _errorContext.Errors.AddAsync(error);
    }
}