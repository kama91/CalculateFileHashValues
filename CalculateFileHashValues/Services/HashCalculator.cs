using System;
using System.IO;
using System.IO.Hashing;
using System.Threading.Channels;
using System.Threading.Tasks;

using CalculateFileHashValues.Models;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.Services;

public sealed class HashCalculator(
    IChannelDataWriter<FileStream> streamCleaner,
    IChannelDataWriter<string> errorService,
    int maxDegreeOfParallelism) : IChannelDataWriter<string>, IChannelDataReader<FileHash>
{
    private const int BufferSize = 1024 * 1024;

    private readonly IChannelDataWriter<FileStream> _streamCleaner =
        streamCleaner ?? throw new ArgumentNullException(nameof(streamCleaner));

    private readonly IChannelDataWriter<string> _errorService =
        errorService ?? throw new ArgumentNullException(nameof(errorService));

    private readonly Channel<string> _inputDataChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(50000)
    {
        AllowSynchronousContinuations = true
    });

    private readonly Channel<FileHash> _transformedDataChannel = Channel.CreateBounded<FileHash>(new BoundedChannelOptions(50000)
    {
        AllowSynchronousContinuations = true
    });

    public ChannelWriter<string> Writer => _inputDataChannel.Writer;

    public ChannelReader<FileHash> Reader => _transformedDataChannel.Reader;

    public async Task Calculate()
    {
        await Task.Yield();
        
        await ReadFilesAndComputeHash();

        _transformedDataChannel.Writer.TryComplete();
        _streamCleaner.Writer.TryComplete();
    }

    private async Task ReadFilesAndComputeHash()
    {
        await Parallel.ForEachAsync(_inputDataChannel.Reader.ReadAllAsync(), new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism

        }, async (path, ct) =>
        {
            FileStream stream = null;
            try
            {
                stream = new(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: BufferSize,
                    options: FileOptions.SequentialScan);

                var hash = new XxHash128();
                hash.Append(stream);
                var bytes = hash.GetCurrentHash();

                await _transformedDataChannel.Writer.WriteAsync(new(path, Convert.ToHexString(bytes)), ct);
            }
            catch (Exception ex)
            {
                await _errorService.Writer.WriteAsync(ex.ToString(), ct);
            }
            finally
            {
                await _streamCleaner.Writer.WriteAsync(stream, ct);
            }
        });
    }
}