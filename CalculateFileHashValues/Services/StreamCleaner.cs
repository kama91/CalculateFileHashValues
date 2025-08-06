using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.Services;

public sealed class StreamCleaner : IChannelDataWriter<FileStream>
{
    private readonly Channel<FileStream> _streams = Channel.CreateBounded<FileStream>(new BoundedChannelOptions(50000)
    {
        AllowSynchronousContinuations = true,
        SingleReader = true
    });

    public ChannelWriter<FileStream> Writer => _streams.Writer;

    public async Task Clean()
    {
        await Task.Yield();
        
        while (await _streams.Reader.WaitToReadAsync())
        {
            await foreach (var stream in _streams.Reader.ReadAllAsync())
            {
                if (stream is not null)
                {
                    await stream.DisposeAsync();
                }
            }
        }
    }
}