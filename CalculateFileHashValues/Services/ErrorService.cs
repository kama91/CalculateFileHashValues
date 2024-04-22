using System.Threading.Channels;
using CalculateFileHashValues.Models;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.Services;

public sealed class ErrorService : IDataWriter<ErrorItem>, IDataReader<ErrorItem>
{
    private readonly Channel<ErrorItem> _errorsChannel = Channel.CreateUnbounded<ErrorItem>();

    public ChannelReader<ErrorItem> Reader => _errorsChannel.Reader;

    public ChannelWriter<ErrorItem> Writer => _errorsChannel.Writer;
}