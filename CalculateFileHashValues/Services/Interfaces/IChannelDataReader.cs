using System.Threading.Channels;

namespace CalculateFileHashValues.Services.Interfaces;

public interface IChannelDataReader<TD>
{
    ChannelReader<TD> Reader { get; }
}