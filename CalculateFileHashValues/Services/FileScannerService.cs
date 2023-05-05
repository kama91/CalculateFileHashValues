using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Services.Interfaces;

using System;
using System.Collections.Generic;
using System.IO;
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

        public async Task ScanDirectories(string directoryPaths)
        {
            if (directoryPaths == null)
            {
                throw new ArgumentNullException(nameof(directoryPaths));
            }

            Console.WriteLine("File scanner service was started");

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

            _dataTransformer.Writer.Complete();

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
                    await WriteError(ex.ToString());
                }
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex)
                {
                    await WriteError(ex.ToString());
                }
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        await _dataTransformer.Writer.WriteAsync(file);
                    }
                }
            }
        }

        private async Task WriteError(string error)
        {
            await _errorService.Writer.WriteAsync(new Error(error));

            Console.Error.WriteLine($"Error: {error}");
        }
    }
}
