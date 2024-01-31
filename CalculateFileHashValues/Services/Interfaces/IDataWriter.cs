using System.Threading.Channels;

namespace CalculateFileHashValues.Services.Interfaces;

public interface IDataWriter<TD>
{
    ChannelWriter<TD> Writer { get; }
}