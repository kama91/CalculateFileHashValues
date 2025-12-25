using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.Services;

public sealed class FileScanner(ErrorService errorService) : IChannelDataReader<string>
{
    private const int MaxDegreeOfParallelism = 4;

    private readonly ErrorService _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));

    private readonly Channel<string> _outputDataChannel = Channel.CreateBounded<string>(
        new BoundedChannelOptions(1000000)
        {
            AllowSynchronousContinuations = true,
            SingleWriter = true
        });

    public ChannelReader<string> Reader => _outputDataChannel.Reader;

    public async Task CollectFilePaths(string directories)
    {
        ArgumentNullException.ThrowIfNull(directories);

        await Task.Yield();

        foreach (var path in directories.Split(','))
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

        _outputDataChannel.Writer.TryComplete();
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

            if (files.Length < MaxDegreeOfParallelism)
            {
                foreach (var file in files)
                {
                    await _outputDataChannel.Writer.WriteAsync(file);
                }
            }
            else
            {
                var parallelOptions = new ParallelOptions
                {
                    CancellationToken = CancellationToken.None,
                    MaxDegreeOfParallelism = MaxDegreeOfParallelism
                };
                await Parallel.ForEachAsync(
                    files,
                    parallelOptions,
                    async (file, ct) => { await _outputDataChannel.Writer.WriteAsync(file, ct); });
            }

            foreach (var str in subDirs)
            {
                dirs.Enqueue(str);
            }
        }
    }
}