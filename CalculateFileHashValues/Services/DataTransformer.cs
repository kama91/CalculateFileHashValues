using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using CalculateFileHashValues.Models;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.Services;

public sealed class DataTransformer<TI, TO>(
    Func<TI, TO> transformAlgorithm,
    IDataWriter<Error> errorService
) : IDataWriter<TI>, IDataReader<TO>
{
    private readonly IDataWriter<Error> _errorService =
        errorService ?? throw new ArgumentNullException(nameof(errorService));

    private readonly Channel<TI> _inputDataChannel = Channel.CreateUnbounded<TI>();

    private readonly Func<TI, TO> _transformAlgorithm =
        transformAlgorithm ?? throw new ArgumentNullException(nameof(transformAlgorithm));

    private readonly Channel<TO> _transformedDataChannel = Channel.CreateUnbounded<TO>();

    public ChannelReader<TO> Reader => _transformedDataChannel.Reader;

    public ChannelWriter<TI> Writer => _inputDataChannel.Writer;

    public async Task Transform()
    {
        Console.WriteLine("Data transformer was started");

        await TransformAndWriteToChannel();

        Console.WriteLine("Data transformer has finished work");
    }

    private async Task TransformAndWriteToChannel()
    {
        while (!_inputDataChannel.Reader.Completion.IsCompleted) await ReadAndWrite();

        await ReadAndWrite();

        _transformedDataChannel.Writer.Complete();
    }

    private async Task ReadAndWrite()
    {
        while (_inputDataChannel.Reader.TryRead(out var filePath))
            try
            {
                var transformedData = _transformAlgorithm.Invoke(filePath);
                await _transformedDataChannel.Writer.WriteAsync(transformedData);
            }
            catch (Exception ex)
            {
                var fullError = ex.ToString();
                await _errorService.Writer.WriteAsync(new Error(fullError));

                await Console.Error.WriteLineAsync($"Error: {fullError}");
            }
    }
}