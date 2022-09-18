using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CalculateFilesHashCodes.Services.Interfaces;

namespace CalculateFilesHashCodes.Services
{
    public class FileScannerService : IDataService<string>
    {
        private readonly IDataService<string> _errorService;
        
        public ServiceStatus Status { get; set; }

        public ConcurrentQueue<string> DataQueue { get; } = new();

        public FileScannerService(IDataService<string> errorService)
        {
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
                        Console.Error.WriteLine($"{path} is not a valid file or directory.");
                    }
                }
                Status = ServiceStatus.Completed;
                Console.WriteLine("FileScannerService has finished work");
            }
            catch (Exception ex)
            {
                _errorService.DataQueue.Enqueue(ex.Message);
                Status = ServiceStatus.Stopped;
                Console.Error.WriteLine($"Error: {ex.Message}");
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
                    _errorService.DataQueue.Enqueue(ex.Message);
                    Console.Error.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        public static void ProcessFile(string path)
        {
            Console.WriteLine($"Processed file '{path}'.");
        }
    }
}
