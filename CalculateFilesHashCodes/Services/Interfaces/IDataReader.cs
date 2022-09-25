using System.Threading.Channels;

namespace CalculateFilesHashCodes.Services.Interfaces
{
    public interface IDataReader<TD>
    {
        ChannelReader<TD> DataReader { get; }
    }
}
