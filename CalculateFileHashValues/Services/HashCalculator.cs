using System;
using System.IO;
using System.IO.Hashing;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using CalculateFileHashValues.Models;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.Services;

public sealed class HashCalculator(
    IChannelDataReader<string> filePathsReader,
    IChannelDataWriter<string> errorService) : IChannelDataReader<FileHash>
{
    private const int Capacity = 100000;
    private const int BufferSize = 1024 * 1024;
    
    private readonly IChannelDataReader<string> _filePathsReader =
        filePathsReader ?? throw new ArgumentNullException(nameof(filePathsReader));

    private readonly IChannelDataWriter<string> _errorService =
        errorService ?? throw new ArgumentNullException(nameof(errorService));

    private readonly Channel<FileHash> _outputDataChannel = Channel.CreateBounded<FileHash>(new BoundedChannelOptions(Capacity)
    {
        AllowSynchronousContinuations = true,
        SingleReader = true
    });
    
    private readonly Channel<FileStreamData> _streamsChannel = Channel.CreateBounded<FileStreamData>(
        new BoundedChannelOptions(Capacity) { AllowSynchronousContinuations = true });

    public ChannelReader<FileHash> Reader => _outputDataChannel.Reader;

    public async Task Calculate() => await Task.WhenAll(ReadFiles(), ComputeHashes());
    
    private async Task ReadFiles()
    {
        await foreach(var path in _filePathsReader.Reader.ReadAllAsync())
        {
                FileStream stream = null;
                try
                {
                    stream = new(
                        path,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        BufferSize,
                        FileOptions.SequentialScan | FileOptions.Asynchronous);

                    await _streamsChannel.Writer.WriteAsync(new(path, stream), CancellationToken.None);
                }
                catch (Exception ex)
                {
                    await _errorService.Writer.WriteAsync(ex.Message, CancellationToken.None);
                    stream?.Dispose();
                }
        }

        _streamsChannel.Writer.TryComplete();
    }

    private async Task ComputeHashes()
    {
        await foreach(var fileStreamData in _streamsChannel.Reader.ReadAllAsync())
        {
            try
            {
                var hash = new XxHash128();
                hash.Append(fileStreamData.DataStream);
                var bytes = hash.GetCurrentHash();

                await _outputDataChannel.Writer.WriteAsync(
                    new(fileStreamData.Path, Convert.ToHexString(bytes)),
                    CancellationToken.None);
            }
            finally
            {
                fileStreamData.DataStream.Dispose();   
            }
        }
        
        _outputDataChannel.Writer.TryComplete();
    }
}