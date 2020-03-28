using System.Collections.Concurrent;
using CalculateFilesHashCodes.Interfaces;
using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Utils;

namespace CalculateFilesHashCodes.Services
{
    public class ErrorService : IDataService<ErrorNode>
    {
        private static ErrorService _instance;
        private static readonly object Lock = new object();

        public StatusService Status { get; set; }
        public ConcurrentQueue<ErrorNode> DataQueue { get; } = new ConcurrentQueue<ErrorNode>();

        public static ErrorService GetCurrentErrorService()
        {
            lock (Lock)
            {
                return _instance ??= new ErrorService();
            }
        }
    }
}
