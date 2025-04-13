using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.Services;

public class StreamCleaner : IDataWriter<FileStream>
{
    private readonly Channel<FileStream> _streams = Channel.CreateUnbounded<FileStream>(new UnboundedChannelOptions
    {
        AllowSynchronousContinuations = true,
        SingleReader = true,
        SingleWriter = true
    });

    public ChannelWriter<FileStream> Writer => _streams.Writer;

    public async Task Clean()
    {
        await Task.Yield();
        
        while (await _streams.Reader.WaitToReadAsync())
        {
            await foreach (var stream in _streams.Reader.ReadAllAsync())
            {
                await stream.DisposeAsync();
            }
        }
    }
}