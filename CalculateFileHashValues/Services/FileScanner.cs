using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CalculateFileHashValues.Models;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.Services;

public sealed class FileScanner(
    IDataWriter<string> dataTransformer,
    ErrorService errorService)
{
    private readonly IDataWriter<string> _dataTransformer =
        dataTransformer ?? throw new ArgumentNullException(nameof(dataTransformer));

    private readonly ErrorService _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));

    public async Task ScanDirectories(string directoryPaths)
    {
        ArgumentNullException.ThrowIfNull(directoryPaths);

        Console.WriteLine("File scanner was started");

        foreach (var path in directoryPaths.Split(','))
        {
            if (Directory.Exists(path))
            {
                await AddFilePathsToDataTransformer(path);
            }
            else
            {
                await Console.Error.WriteLineAsync($"{path} is not exists.");
            }
        }

        _dataTransformer.Writer.TryComplete();

        Console.WriteLine("File scanner has finished work");
    }

    private async Task AddFilePathsToDataTransformer(string path)
    {
        var paths = new Queue<string>();
        paths.Enqueue(path);

        while (paths.Count > 0)
        {
            path = paths.Dequeue();
            try
            {
                foreach (var subDir in Directory.GetDirectories(path))
                {
                    paths.Enqueue(subDir);
                }
            }
            catch (Exception ex)
            {
                await WriteError(ex.ToString());
            }

            try
            {
               foreach (var file in Directory.GetFiles(path))
               {
                   await _dataTransformer.Writer.WriteAsync(file);
               }
            }
            catch (Exception ex)
            {
                await WriteError(ex.ToString());
            }
        }
    }

    private async Task WriteError(string error)
    {
        await _errorService.Writer.WriteAsync(new ErrorItem
        {
            Description = error
        });

        await Console.Error.WriteLineAsync($"Error: {error}");
    }
}