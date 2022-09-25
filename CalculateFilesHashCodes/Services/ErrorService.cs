using System.Collections.Concurrent;

using CalculateFilesHashCodes.Models;

namespace CalculateFilesHashCodes.Services
{
    public sealed class ErrorService
    {
        public DataReceivingStatus Status { get; set; }
        public ConcurrentQueue<string> ErrorsQueue { get; } = new();
    }
}
