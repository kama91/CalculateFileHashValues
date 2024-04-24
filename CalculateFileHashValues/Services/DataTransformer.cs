using System;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using CalculateFileHashValues.Extensions;
using CalculateFileHashValues.Models;
using CalculateFileHashValues.Services.Interfaces;

namespace CalculateFileHashValues.Services;

public sealed class DataTransformer<TI, TO>(
    Func<TI, TO> transformAlgorithm,
    IDataWriter<ErrorItem> errorService
) : IDataWriter<TI>, IDataReader<TO>
{
    private readonly IDataWriter<ErrorItem> _errorService =
        errorService ?? throw new ArgumentNullException(nameof(errorService));

    private readonly Channel<TI> _inputDataChannel = Channel.CreateUnbounded<TI>(new UnboundedChannelOptions
    {
        AllowSynchronousContinuations = true,
        SingleReader = true,
        SingleWriter = true,
    });
    
    private readonly Channel<TO> _transformedDataChannel = Channel.CreateUnbounded<TO>(new UnboundedChannelOptions
    {
        AllowSynchronousContinuations = true,
        SingleReader = true,
        SingleWriter = true,
    });

    private readonly Func<TI, TO> _transformAlgorithm =
        transformAlgorithm ?? throw new ArgumentNullException(nameof(transformAlgorithm));

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
        await _inputDataChannel.Reader.ProcessData(ReadAndWrite);

        _transformedDataChannel.Writer.TryComplete();
    }

    private async Task ReadAndWrite()
    {
        await foreach (var path in _inputDataChannel.Reader.ReadAllAsync())
        {
            try
            {
                await _transformedDataChannel.Writer.WriteAsync(_transformAlgorithm.Invoke(path));
            }
            catch (Exception ex)
            {
                var fullError = ex.ToString();
                await _errorService.Writer.WriteAsync(new ErrorItem
                {
                    Description = fullError
                });
                await Console.Error.WriteLineAsync($"Error: {fullError}");
            }
        }
    }
}