using System.Collections.Concurrent;
using CalculateFilesHashCodes.Common;
using CalculateFilesHashCodes.Services;

namespace CalculateFilesHashCodes.Interfaces
{
    public interface IDataService<T>
    {
        /// <summary>
        /// Service operation status
        /// </summary>
        ServiceStatus Status { get; set; }

        /// <summary>
        /// The collection of data that service fills
        /// </summary>
        ConcurrentQueue<T> DataQueue { get; }
    }
}
