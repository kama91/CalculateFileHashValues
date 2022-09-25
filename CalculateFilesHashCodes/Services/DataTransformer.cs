using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CalculateFilesHashCodes.Common;
using CalculateFilesHashCodes.DAL;
using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Services.Interfaces;

namespace CalculateFilesHashCodes.Services
{
    public class DataTransformer : IDataService<string, FileHashItem>
    {
        private readonly ErrorService _errorService;
        private readonly ConcurrentQueue<string> _inputQueue = new();
        private readonly ConcurrentQueue<FileHashItem> _outputQueue = new();
        private readonly Func<string, FileHashItem> _transformAlgorithm;

        public WorkStatus WorkStatus { get; private set; }

        public DataReceivingStatus DataReceivingStatus { get; set; }

        public ConcurrentQueue<string> InputData => _inputQueue;

        public ConcurrentQueue<FileHashItem> OutputData => _outputQueue;

        public DataTransformer(
            Func<string, FileHashItem> transformAlgorithm,
            ErrorService errorService
            )
        {
            _transformAlgorithm = transformAlgorithm ?? throw new ArgumentNullException(nameof(transformAlgorithm));
            _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
        }

        public async Task Transform()
        {
            WorkStatus = WorkStatus.Running;
            
            var task = this.HandlingDataUntilReceivingCompleted(AddTransformedDataToOutputQueue);
            
            await this.HandlingDataUntilReceivingCompleted(AddTransformedDataToOutputQueue);

            WorkStatus = WorkStatus.Completed;

            Console.WriteLine("FileHashService has finished work");
        }

        private void AddTransformedDataToOutputQueue()
        {
            while (_inputQueue.TryDequeue(out var filePath))
            {
                try
                {
                    _outputQueue.Enqueue(_transformAlgorithm.Invoke(filePath));
                }
                catch (Exception ex)
                {
                    _errorService.ErrorsQueue.Enqueue(ex.Message);

                    Console.Error.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}
