using System;
using System.Threading.Channels;
using System.Threading.Tasks;

using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Services.Interfaces;

namespace CalculateFilesHashCodes.Services
{
    public class DataTransformer<TI, TO> : IDataWriter<TI>, IDataReader<TO>
    {
        private readonly IDataWriter<string> _errorService;
        private readonly Channel<TI> _inputDataChannel = Channel.CreateUnbounded<TI>();
        private readonly Channel<TO> _tranformedDataChannel = Channel.CreateUnbounded<TO>();
        private readonly Func<TI, TO> _transformAlgorithm;

        public ChannelWriter<TI> DataWriter => _inputDataChannel.Writer;

        public ChannelReader<TO> DataReader => _tranformedDataChannel.Reader;

        public DataTransformer(
            Func<TI, TO> transformAlgorithm,
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
