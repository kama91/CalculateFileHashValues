using System;
using System.IO;
using System.IO.Hashing;
using System.Threading.Channels;
using System.Threading.Tasks;
using CalculateFileHashValues.DataAccess.Models;
using CalculateFileHashValues.Extensions;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.Services;

public sealed class DataTransformer(
    IDataWriter<FileStream> streamCleaner,
    IDataWriter<Error> errorService) : IDataWriter<string>, IDataReader<FileHash>
{
    private readonly IDataWriter<FileStream> _streamCleaner = 
        streamCleaner ?? throw new ArgumentNullException(nameof(streamCleaner));

    private readonly IDataWriter<Error> _errorService =
        errorService ?? throw new ArgumentNullException(nameof(errorService));

    private readonly Channel<string> _inputDataChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
    {
        AllowSynchronousContinuations = true,
    });
    
    private readonly Channel<FileHash> _transformedDataChannel = Channel.CreateUnbounded<FileHash>(new UnboundedChannelOptions
    {
        AllowSynchronousContinuations = true,
    });

    public ChannelWriter<string> Writer => _inputDataChannel.Writer;

    public ChannelReader<FileHash> Reader => _transformedDataChannel.Reader;

    public async Task Transform()
    {
        await Task.Yield();
        
        var processorCount = Environment.ProcessorCount;
        var tasks = new Task[processorCount];

        for (var i = 0; i < processorCount; ++i)
        {
            tasks[i] = _inputDataChannel.Reader.ProcessData(TransformData);
        }

        await Task.WhenAll(tasks);
        
        _transformedDataChannel.Writer.TryComplete();
        _streamCleaner.Writer.TryComplete();
    }
    
    private async Task TransformData()
    {
        while (_inputDataChannel.Reader.TryRead(out var path))
        {
            try
            {
                var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                var hash = new XxHash128();
                await hash.AppendAsync(stream);
                var bytes = hash.GetCurrentHash();
                
                await _transformedDataChannel.Writer.WriteAsync(new FileHash(path, Convert.ToHexString(bytes)));
                
                await _streamCleaner.Writer.WriteAsync(stream);
            }
            catch (Exception ex)
            {
                await _errorService.Writer.WriteAsync(new Error(ex.ToString()));
            }
        }
    }
}