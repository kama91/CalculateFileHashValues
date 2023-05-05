using System.Threading.Channels;

namespace CalculateFilesHashCodes.Services.Interfaces
{
    public interface IDataWriter<TD>
    {
        ChannelWriter<TD> Writer { get; }
    }
}
