using CalculateFilesHashCodes.Services.Interfaces;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFilesHashCodes.Services
{
    public class FileScannerService
    {
        private readonly IDataWriter<string> _dataTransformer;
        private readonly ErrorService _errorService;
        
        public FileScannerService(
            IDataWriter<string> dataTransformer,
            ErrorService errorService)
        {
            _dataTransformer = dataTransformer ?? throw new ArgumentNullException(nameof(dataTransformer));
            _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
        }

        public async Task StartScanDirectory(string directoryPaths)
        {
            if (directoryPaths == null)
            {
                throw new ArgumentNullException(nameof(directoryPaths));  
            }

            await ScanPaths(directoryPaths);
        }

        private async Task ScanPaths(string directoriesPaths)
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
                        await AddFilesToQueue(path);
                    }
                    else
                    {
                        Console.Error.WriteLine($"{path} is not a valid file or directory.");
                    }
                }

                _dataTransformer.DataWriter.Complete();
                
                Console.WriteLine("FileScannerService has finished work");
            }
            catch (Exception ex)
            {
                await _errorService.DataWriter.WriteAsync(ex.Message);
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }

        public async Task AddFilesToQueue(string targetDirectory)
        {
            try
            {
                foreach (var fileName in Directory.GetFiles(targetDirectory))
                {
                    await _dataTransformer.DataWriter.WriteAsync(fileName);
                }

                foreach (var subDirectory in Directory.GetDirectories(targetDirectory))
                {
                    try
                    {
                        await AddFilesToQueue(subDirectory);
                    }
                    catch (Exception ex)
                    {
                        await _errorService.DataWriter.WriteAsync(ex.Message);
                        
                        Console.Error.WriteLine($"Error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                await _errorService.DataWriter.WriteAsync(ex.Message);
                
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }

        public static void ProcessFile(string path)
        {
            Console.WriteLine($"Processed file '{path}'.");
        }
    }
}
