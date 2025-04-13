using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CalculateFileHashValues.DataAccess.Models;
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
        await Task.Yield();
        
        ArgumentNullException.ThrowIfNull(directoryPaths);

        foreach (var path in directoryPaths.Split(','))
        {
            if (Directory.Exists(path))
            {
                await ParallelScan(path);
            }
            else
            {
                await Console.Error.WriteLineAsync($"{path} is not exists.");
            }
        }

        _dataTransformer.Writer.TryComplete();
    }

    private async Task WriteError(string error) => await _errorService.Writer.WriteAsync(new Error(error));
    
    private async Task ParallelScan(string root)
    {
        var processorCount = Environment.ProcessorCount;

        var dirs = new Queue<string>();

        dirs.Enqueue(root);

        while (dirs.Count > 0)
        {
            var currentDir = dirs.Dequeue();
            
            string[] subDirs = [];
            string[] files = [];

            try
            {
                subDirs = Directory.GetDirectories(currentDir);
            }
            catch (Exception ex)
            {
                await WriteError(ex.ToString());
            }

            try
            {
                files = Directory.GetFiles(currentDir);
            }
            catch (Exception ex)
            {
                await WriteError(ex.ToString());
            }

            if (files.Length < processorCount)
            {
                foreach (var file in files)
                {
                    await _dataTransformer.Writer.WriteAsync(file);
                }
            }
            else
            {
                var parallelOptions = new ParallelOptions
                {
                    CancellationToken = CancellationToken.None,
                    MaxDegreeOfParallelism = processorCount,
                };
                await Parallel.ForEachAsync(files, parallelOptions,async (file, ct) =>
                { 
                    await _dataTransformer.Writer.WriteAsync(file, ct);
                });
            }

            foreach (var str in subDirs)
            {
                dirs.Enqueue(str);
            }
        }
    }
}