using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Services.Interfaces;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CalculateFilesHashCodes.Services
{
    public class DataTransformer<TI, TO> : IDataWriter<TI>, IDataReader<TO>
    {
        private readonly IDataWriter<Error> _errorService;
        private readonly Channel<TI> _inputDataChannel = Channel.CreateUnbounded<TI>();
        private readonly Channel<TO> _tranformedDataChannel = Channel.CreateUnbounded<TO>();
        private readonly Func<TI, TO> _transformAlgorithm;

        public ChannelWriter<TI> Writer => _inputDataChannel.Writer;

        public ChannelReader<TO> Reader => _tranformedDataChannel.Reader;

        public DataTransformer(
            Func<TI, TO> transformAlgorithm,
            IDataWriter<Error> errorService
            )
        {
            _transformAlgorithm = transformAlgorithm ?? throw new ArgumentNullException(nameof(transformAlgorithm));
            _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
        }

        public async Task Transform()
        {
            Console.WriteLine("Data transformer was started");

            await TransformAndWriteToChannel();

            Console.WriteLine("Data transformer successfully completed");
        }

        private async Task TransformAndWriteToChannel()
        {
            while (!_inputDataChannel.Reader.Completion.IsCompleted)
            {
                await ReadAndWrite();
            }

            await ReadAndWrite();

            _tranformedDataChannel.Writer.Complete();
            _errorService.Writer.Complete();
        }

        private async Task ReadAndWrite()
        {
            while (_inputDataChannel.Reader.TryRead(out var filePath))
            {
                try
                {
                    await _tranformedDataChannel.Writer.WriteAsync(_transformAlgorithm.Invoke(filePath));
                }
                catch (Exception ex)
                {
                    var fullError = ex.ToString();
                    await _errorService.Writer.WriteAsync(new Error(fullError));

                    Console.Error.WriteLine($"Error: {fullError}");
                }
            }
        }
    }
}
