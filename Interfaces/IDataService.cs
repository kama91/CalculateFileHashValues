using System.Collections.Concurrent;
using CalculateFilesHashCodes.Common;

namespace CalculateFilesHashCodes.Interfaces
{
    public interface IDataService<T>
    {
        /// <summary>
        /// Service operation status
        /// </summary>
        StatusService Status { get; set; }

        /// <summary>
        /// The collection of data that service fills
        /// </summary>
        ConcurrentQueue<T> DataQueue { get; }
    }
}
