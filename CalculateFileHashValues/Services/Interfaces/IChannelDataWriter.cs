using System.Threading.Channels;

namespace CalculateFileHashValues.Services.Interfaces;

public interface IChannelDataWriter<TD>
{
    ChannelWriter<TD> Writer { get; }
}