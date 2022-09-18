using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using CalculateFilesHashCodes.Common;
using CalculateFilesHashCodes.HashCodeAlgorithm.Interfaces;
using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Services.Interfaces;

namespace CalculateFilesHashCodes.Services
{
    public class FileHashService : IDataService<FileHashItem>
    {
        private readonly IDataService<string> _fileScannerService;
        private readonly IDataService<string> _errorService;
        private readonly IHashCodeAlgorithm _hashCodeAlgorithm;

        public ServiceStatus Status { get; set; }
        public ConcurrentQueue<FileHashItem> DataQueue { get; } = new();
        
        public FileHashService(
            IDataService<string> fileScannerService,
            IDataService<string> errorService,
            IHashCodeAlgorithm hashCodeAlgorithm
            )
        {
            _fileScannerService = fileScannerService ?? throw new ArgumentNullException(nameof(fileScannerService));
            _hashCodeAlgorithm = hashCodeAlgorithm ?? throw new ArgumentNullException(nameof(hashCodeAlgorithm));
            _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
        }

        public async Task StartCalculate()
        {
            Status = ServiceStatus.Running;

            await _fileScannerService.HandlingData(AddHashToQueue);

            Status = ServiceStatus.Completed;
            Console.WriteLine("FileHashService has finished work");
        }

        private void AddHashToQueue()
        {
            while (_fileScannerService.DataQueue.TryDequeue(out var filePath))
            {
                try
                {
                    DataQueue.Enqueue(new FileHashItem(filePath, BitConverter.ToString(GetHashCode(filePath))));
                }
                catch (Exception ex)
                {
                    _errorService.DataQueue.Enqueue(ex.Message);
                    Console.Error.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private byte[] GetHashCode(string filePath)
        {
            return _hashCodeAlgorithm.ComputeHash(filePath);
        }
    }
}
