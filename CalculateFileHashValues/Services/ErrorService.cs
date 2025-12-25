using System.Threading.Channels;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.Services;

public sealed class ErrorService : IChannelDataWriter<string>, IChannelDataReader<string>
{
    private readonly Channel<string> _errorsChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(10000)
    {
        AllowSynchronousContinuations = true,
        SingleReader = true
    });

    public ChannelReader<string> Reader => _errorsChannel.Reader;

    public ChannelWriter<string> Writer => _errorsChannel.Writer;
}