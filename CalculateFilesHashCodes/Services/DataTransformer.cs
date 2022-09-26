using System;
using System.Threading.Channels;
using System.Threading.Tasks;

using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Services.Interfaces;

namespace CalculateFilesHashCodes.Services
{
    public class DataTransformer : IDataWriter<string>, IDataReader<FileHashItem>
    {
        private readonly IDataWriter<string> _errorService;
        private readonly Channel<string> _inputDataChannel = Channel.CreateUnbounded<string>();
        private readonly Channel<FileHashItem> _tranformedDataChannel = Channel.CreateUnbounded<FileHashItem>();
        private readonly Func<string, FileHashItem> _transformAlgorithm;

        public ChannelWriter<string> DataWriter => _inputDataChannel.Writer;

        public ChannelReader<FileHashItem> DataReader => _tranformedDataChannel.Reader;

        public DataTransformer(
            Func<string, FileHashItem> transformAlgorithm,
            IDataWriter<string> errorService
            )
        {
            _transformAlgorithm = transformAlgorithm ?? throw new ArgumentNullException(nameof(transformAlgorithm));
            _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
        }

        public async Task TransformAsync()
        {
            Console.WriteLine("Data transformer was started");

            await TransformAndWriteToChannel();

            Console.WriteLine("Data transformered successfully completed");
        }

        private async Task TransformAndWriteToChannel()
        {
            while (!_inputDataChannel.Reader.Completion.IsCompleted && 
                _inputDataChannel.Reader.TryRead(out var filePath))
            {
                try
                {
                    await _tranformedDataChannel.Writer.WriteAsync(_transformAlgorithm.Invoke(filePath));
                }
                catch (Exception ex)
                {
                    await _errorService.DataWriter.WriteAsync(ex.Message);

                    Console.Error.WriteLine($"Error: {ex.Message}");
                }
            }

            _tranformedDataChannel.Writer.Complete();
        }
    }
}
