using System.Collections.Concurrent;
using CalculateFilesHashCodes.Common;
using CalculateFilesHashCodes.Interfaces;
using CalculateFilesHashCodes.Models;

namespace CalculateFilesHashCodes.Services
{
    public sealed class ErrorService : IDataService<ErrorNode>
    {
        private static ErrorService _instance;
        private static readonly object Lock = new object();

        private ErrorService()
        {

        }

        public StatusService Status { get; set; }
        public ConcurrentQueue<ErrorNode> DataQueue { get; } = new ConcurrentQueue<ErrorNode>();

        public static ErrorService CurrentErrorService
        {
            get
            {
                if (_instance != null) return _instance;
                lock (Lock)
                {
                    _instance ??= new ErrorService();
                }

                return _instance;
            }
        }
    }
}
