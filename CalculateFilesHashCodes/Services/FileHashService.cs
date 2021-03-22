using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using CalculateFilesHashCodes.Common;
using CalculateFilesHashCodes.HashCodeAlgorithm.Interfaces;
using CalculateFilesHashCodes.Interfaces;
using CalculateFilesHashCodes.Models;

namespace CalculateFilesHashCodes.Services
{
    public class FileHashService : IDataService<FileNode>
    {
        private readonly IDataService<string> _fileScannerService;
        private readonly IDataService<ErrorNode> _errorService;
        private readonly IHashCodeAlgorithm _hashCodeAlgorithm;

        public ServiceStatus Status { get; set; }
        public ConcurrentQueue<FileNode> DataQueue { get; } = new();
        
        public FileHashService(
            IDataService<string> fileScannerService,
            IDataService<ErrorNode> errorService,
            IHashCodeAlgorithm hashCodeAlgorithm
            )
        {
            _fileScannerService = fileScannerService ?? throw new ArgumentNullException(nameof(fileScannerService));
            _hashCodeAlgorithm = hashCodeAlgorithm ?? throw new ArgumentNullException(nameof(hashCodeAlgorithm));
            _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
        }

        public Task StartCalculate()
        {
            return Task.Run(CalculateHashCodeAndAddToQueue);
        }

        public void CalculateHashCodeAndAddToQueue()
        {
            Status = ServiceStatus.Running;

            _fileScannerService.HandlingData(AddHashToQueue);

            Status = ServiceStatus.Complete;
            Console.WriteLine("FileHashService has finished work");
        }

        private void AddHashToQueue()
        {
            while (_fileScannerService.DataQueue.TryDequeue(out var filePath))
            {
                try
                {
                    DataQueue.Enqueue(new FileNode(filePath, BitConverter.ToString(GetHashCode(filePath))));
                }
                catch (Exception ex)
                {
                    _errorService.DataQueue.Enqueue(new ErrorNode {Info = ex.Source + ex.Message + ex.StackTrace});
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private byte[] GetHashCode(string filePath)
        {
            return _hashCodeAlgorithm.ComputeHash(filePath);
        }
    }
}
