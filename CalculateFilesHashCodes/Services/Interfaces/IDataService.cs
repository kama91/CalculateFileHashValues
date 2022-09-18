using System.Collections.Concurrent;

namespace CalculateFilesHashCodes.Services.Interfaces
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
