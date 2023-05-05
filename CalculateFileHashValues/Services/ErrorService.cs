using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Services.Interfaces;

using System.Threading.Channels;

namespace CalculateFilesHashCodes.Services
{
    public sealed class ErrorService : IDataWriter<Error>, IDataReader<Error>
    {
        private readonly Channel<Error> _errorsChannel = Channel.CreateUnbounded<Error>();

        public ChannelReader<Error> Reader => _errorsChannel.Reader;

        public ChannelWriter<Error> Writer => _errorsChannel.Writer;
    }
}
