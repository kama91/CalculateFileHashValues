using CalculateFilesHashCodes.Services.Interfaces;

using System;
using System.Collections.Generic;
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

        public async Task ScanDirectoriesAsync(string directoryPaths)
        {
            if (directoryPaths == null)
            {
                throw new ArgumentNullException(nameof(directoryPaths));  
            }

            foreach (var path in directoryPaths.Split(','))
            {
                if (Directory.Exists(path))
                {
                    await AddFilePathsToDataTransformer(path);
                }
                else
                {
                    Console.Error.WriteLine($"{path} is not exists.");
                }
            }

            _dataTransformer.DataWriter.Complete();

            Console.WriteLine("File scanner service has finished work");
        }

        private async Task AddFilePathsToDataTransformer(string path)
        {
            var paths = new Queue<string>();
            paths.Enqueue(path);

            while (paths.Count > 0)
            {
                path = paths.Dequeue();
                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                    {
                        paths.Enqueue(subDir);
                    }
                }
                catch (Exception ex)
                {
                    await WriteError(ex.Message);
                }
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex)
                {
                    await WriteError(ex.Message);
                }
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        await _dataTransformer.DataWriter.WriteAsync(file);
                    }
                }
            }
        }

        private async Task WriteError(string error)
        {
            await _errorService.DataWriter.WriteAsync(error);

            Console.Error.WriteLine($"Error: {error}");
        }
    }
}
