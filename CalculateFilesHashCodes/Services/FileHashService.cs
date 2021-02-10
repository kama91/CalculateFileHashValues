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
        private readonly IHashCodeAlgorithm _hashCodeAlgorithm;

        public StatusService Status { get; set; }
        public ConcurrentQueue<FileNode> DataQueue { get; } = new ConcurrentQueue<FileNode>();
        
        public FileHashService(
            IDataService<string> fileScannerService,
            IHashCodeAlgorithm hashCodeAlgorithm)
        {
            _fileScannerService = fileScannerService ?? throw new ArgumentNullException(nameof(fileScannerService));
            _hashCodeAlgorithm = hashCodeAlgorithm ?? throw new ArgumentNullException(nameof(hashCodeAlgorithm));
        }

        public Task StartCalculate()
        {
            return Task.Run(CalculateHashCodeAndAddToQueue);
        }

        public void CalculateHashCodeAndAddToQueue()
        {
            Status = StatusService.Running;

            _fileScannerService.HandlingData(AddHashToQueue);

            Status = StatusService.Complete;
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
                    ErrorService.CurrentErrorService.DataQueue.Enqueue(new ErrorNode {Info = ex.Source + ex.Message + ex.StackTrace});
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private byte[] GetHashCode(string filePath)
        {
            using var fileStream = File.OpenRead(filePath);
            
            return _hashCodeAlgorithm.ComputeHash(fileStream);
        }
    }
}
