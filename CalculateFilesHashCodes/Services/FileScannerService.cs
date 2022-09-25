using CalculateFilesHashCodes.Models;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFilesHashCodes.Services
{
    public class FileScannerService
    {
        private readonly DataTransformer _dataTransformer;
        private readonly ErrorService _errorService;
        
        public DataReceivingStatus Status { get; set; }

        public FileScannerService(
            DataTransformer dataTransformer,
            ErrorService errorService)
        {
            this._dataTransformer = dataTransformer ?? throw new ArgumentNullException(nameof(dataTransformer));
            _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
        }

        public async Task StartScanDirectory(string directoryPaths)
        {
            if (directoryPaths == null)
            {
                throw new ArgumentNullException(nameof(directoryPaths));  
            }

            await Task.Run(() => ScanPaths(directoryPaths));
        }

        private void ScanPaths(string directoriesPaths)
        {
            try
            {
                foreach (var path in directoriesPaths.Split(',').ToList())
                {
                    if (File.Exists(path))
                    {
                        ProcessFile(path);
                    }
                    else if (Directory.Exists(path))
                    {
                        _dataTransformer.DataReceivingStatus = DataReceivingStatus.Running;
                        AddFilesToQueue(path);
                    }
                    else
                    {
                        Console.Error.WriteLine($"{path} is not a valid file or directory.");
                    }
                }

                _dataTransformer.DataReceivingStatus = DataReceivingStatus.Completed;
                
                Console.WriteLine("FileScannerService has finished work");
            }
            catch (Exception ex)
            {
                _errorService.ErrorsQueue.Enqueue(ex.Message);
                _dataTransformer.DataReceivingStatus = DataReceivingStatus.Stopped;
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }

        public void AddFilesToQueue(string targetDirectory)
        {
            try
            {
                foreach (var fileName in Directory.GetFiles(targetDirectory))
                {
                    _dataTransformer.InputData.Enqueue(fileName);
                    throw new Exception("My Exception");
                }

                foreach (var subDirectory in Directory.GetDirectories(targetDirectory))
                {
                    try
                    {
                        AddFilesToQueue(subDirectory);
                    }
                    catch (Exception ex)
                    {
                        _errorService.ErrorsQueue.Enqueue(ex.Message);
                        
                        Console.Error.WriteLine($"Error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _errorService.ErrorsQueue.Enqueue(ex.Message);
                
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }

        public static void ProcessFile(string path)
        {
            Console.WriteLine($"Processed file '{path}'.");
        }
    }
}
