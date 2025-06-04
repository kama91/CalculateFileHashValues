using System;
using System.IO;
using System.IO.Hashing;
using System.Threading.Channels;
using System.Threading.Tasks;

using CalculateFileHashValues.Extensions;
using CalculateFileHashValues.Models;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.Services;

public sealed class HashCalculator(
    IDataWriter<FileStream> streamCleaner,
    IDataWriter<string> errorService) : IDataWriter<string>, IDataReader<FileHash>
{
    private const int BufferSize = 1024 * 1024;
    private readonly int _maxDegreeOfParallelism = Environment.ProcessorCount;

    private readonly IDataWriter<FileStream> _streamCleaner =
        streamCleaner ?? throw new ArgumentNullException(nameof(streamCleaner));

    private readonly IDataWriter<string> _errorService =
        errorService ?? throw new ArgumentNullException(nameof(errorService));

    private readonly Channel<string> _inputDataChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
    {
        AllowSynchronousContinuations = true
    });

    private readonly Channel<FileHash> _transformedDataChannel = Channel.CreateUnbounded<FileHash>(new UnboundedChannelOptions
    {
        AllowSynchronousContinuations = true
    });

    public ChannelWriter<string> Writer => _inputDataChannel.Writer;

    public ChannelReader<FileHash> Reader => _transformedDataChannel.Reader;

    public async Task Calculate()
    {
        await Task.Yield();

        await _inputDataChannel.Reader.ProcessData(ReadFilesAndComputeHash);

        _transformedDataChannel.Writer.TryComplete();
        _streamCleaner.Writer.TryComplete();
    }

    private async Task ReadFilesAndComputeHash()
    {
        await Parallel.ForEachAsync(_inputDataChannel.Reader.ReadAllAsync(), new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxDegreeOfParallelism,

        }, async (path, ct) =>
        {
            try
            {
                var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: BufferSize,
                    options: FileOptions.SequentialScan);

                var hash = new XxHash128();
                hash.Append(stream);
                var bytes = hash.GetCurrentHash();

                await _transformedDataChannel.Writer.WriteAsync(new FileHash(path, Convert.ToHexString(bytes)), ct);
                await _streamCleaner.Writer.WriteAsync(stream, ct);
            }
            catch (Exception ex)
            {
                await _errorService.Writer.WriteAsync(ex.ToString(), ct);
            }
        });
    }
}