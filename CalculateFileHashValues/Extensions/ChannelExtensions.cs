using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CalculateFileHashValues.Extensions;

public static class ChannelExtensions
{
    public static async Task ProcessData<T>(this ChannelReader<T> reader, Func<Task> function)
    {
        while (await reader.WaitToReadAsync())
        {
            await function.Invoke();
        }

        await function.Invoke();
    }
}