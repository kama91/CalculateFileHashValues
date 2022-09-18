using System.Collections.Concurrent;

using CalculateFilesHashCodes.Services.Interfaces;

namespace CalculateFilesHashCodes.Services
{
    public sealed class ErrorService : IDataService<string>
    {
        public ServiceStatus Status { get; set; }
        public ConcurrentQueue<string> DataQueue { get; } = new();
    }
}
