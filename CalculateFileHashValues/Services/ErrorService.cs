using System.Threading.Channels;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.Services;

public sealed class ErrorService : IDataWriter<string>, IDataReader<string>
{
    private readonly Channel<string> _errorsChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
    {
        AllowSynchronousContinuations = true,
        SingleReader = true
    });

    public ChannelReader<string> Reader => _errorsChannel.Reader;

    public ChannelWriter<string> Writer => _errorsChannel.Writer;
}