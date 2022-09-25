using CalculateFilesHashCodes.Models;

using System.Collections.Concurrent;

namespace CalculateFilesHashCodes.Services.Interfaces
{
    public interface IDataService<TI, TO>
    {
        WorkStatus WorkStatus { get; }

        DataReceivingStatus DataReceivingStatus { get; set; }

        ConcurrentQueue<TI> InputData { get; } 

        ConcurrentQueue<TO> OutputData { get; }
    }
}
