using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.Services;

public sealed class FileScanner(
    IDataWriter<string> dataTransformer,
    ErrorService errorService,
    int maxDegreeOfParallelism)
{
    private readonly IDataWriter<string> _dataTransformer =
        dataTransformer ?? throw new ArgumentNullException(nameof(dataTransformer));

    private readonly ErrorService _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));

    private readonly int _maxDegreeOfParallelism = maxDegreeOfParallelism;

    public async Task ScanDirectories(string directoryPaths)
    {
        ArgumentNullException.ThrowIfNull(directoryPaths);
        
        await Task.Yield();

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

    private async Task WriteError(string error) => await _errorService.Writer.WriteAsync(error);
    
    private async Task ParallelScan(string root)
    {
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

            if (files.Length < _maxDegreeOfParallelism)
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
                    MaxDegreeOfParallelism = _maxDegreeOfParallelism,
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