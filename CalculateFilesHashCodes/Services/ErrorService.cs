using CalculateFilesHashCodes.Services.Interfaces;

using System.Threading.Channels;

namespace CalculateFilesHashCodes.Services
{
    public sealed class ErrorService : IDataWriter<string>, IDataReader<string>
    {
        private readonly Channel<string> _errorsChannel = Channel.CreateUnbounded<string>();

        public ChannelReader<string> DataReader => _errorsChannel.Reader;

        public ChannelWriter<string> DataWriter => _errorsChannel.Writer;
    }
}
