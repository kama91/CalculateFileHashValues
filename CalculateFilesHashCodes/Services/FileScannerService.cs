using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFilesHashCodes.Interfaces;
using CalculateFilesHashCodes.Models;

namespace CalculateFilesHashCodes.Services
{
    public class FileScannerService : IDataService<string>
    {
        private readonly IDataService<ErrorNode> _errorService;
        
        public ServiceStatus Status { get; set; }

        public ConcurrentQueue<string> DataQueue { get; } = new();

        public FileScannerService(IDataService<ErrorNode> errorService)
        {
            _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
        }

        public Task StartScanDirectory(string directoriesPaths)
        {
            return string.IsNullOrEmpty(directoriesPaths) ? Task.FromException(new Exception("Directories paths is null or empty")) : Task.Run(() => ScanPaths(directoriesPaths));
        }

        private void ScanPaths(string directoriesPaths)
        {
            try
            {
                Status = ServiceStatus.Running;
                foreach (var path in directoriesPaths.Split(',').ToList())
                {
                    if (File.Exists(path))
                    {
                        ProcessFile(path);
                    }
                    else if (Directory.Exists(path))
                    {
                        AddFilesToQueue(path);
                    }
                    else
                    {
                        Console.WriteLine($"{path} is not a valid file or directory.");
                    }
                }
                Status = ServiceStatus.Complete;
                Console.WriteLine("FileScannerService has finished work");
            }
            catch (Exception ex)
            {
                _errorService.DataQueue.Enqueue(new ErrorNode {Info = ex.Source + ex.Message + ex.StackTrace});
                Status = ServiceStatus.Stopped;
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public void AddFilesToQueue(string targetDirectory)
        {
            foreach (var fileName in Directory.GetFiles(targetDirectory))
            {
                DataQueue.Enqueue(fileName);
            }
            
            foreach (var subDirectory in Directory.GetDirectories(targetDirectory))
            {
                try
                {
                    AddFilesToQueue(subDirectory);
                }
                catch (Exception ex)
                {
                    _errorService.DataQueue.Enqueue(new ErrorNode { Info = ex.Source + ex.Message + ex.StackTrace });
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        public void ProcessFile(string path)
        {
            Console.WriteLine($"Processed file '{path}'.");
        }
    }
}
