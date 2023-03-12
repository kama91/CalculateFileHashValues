using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Services.Interfaces;

using System.Threading.Channels;

namespace CalculateFilesHashCodes.Services
{
    public sealed class ErrorService : IDataWriter<Error>, IDataReader<Error>
    {
        private readonly Channel<Error> _errorsChannel = Channel.CreateUnbounded<Error>();

        public ChannelReader<Error> DataReader => _errorsChannel.Reader;

        public ChannelWriter<Error> DataWriter => _errorsChannel.Writer;
    }
}
