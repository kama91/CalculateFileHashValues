using System.Collections.Concurrent;
using CalculateFilesHashCodes.Interfaces;
using CalculateFilesHashCodes.Models;

namespace CalculateFilesHashCodes.Services
{
    public sealed class ErrorService : IDataService<ErrorNode>
    {
        public ServiceStatus Status { get; set; }
        public ConcurrentQueue<ErrorNode> DataQueue { get; } = new();
    }
}
