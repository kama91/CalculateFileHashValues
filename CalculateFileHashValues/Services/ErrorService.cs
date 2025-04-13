using System.Threading.Channels;
using CalculateFileHashValues.DataAccess.Models;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.Services;

public sealed class ErrorService : IDataWriter<Error>, IDataReader<Error>
{
    private readonly Channel<Error> _errorsChannel = Channel.CreateUnbounded<Error>();

    public ChannelReader<Error> Reader => _errorsChannel.Reader;

    public ChannelWriter<Error> Writer => _errorsChannel.Writer;
}