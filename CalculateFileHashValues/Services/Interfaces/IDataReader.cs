using System.Threading.Channels;

namespace CalculateFileHashValues.Services.Interfaces;

public interface IDataReader<TD>
{
    ChannelReader<TD> Reader { get; }
}