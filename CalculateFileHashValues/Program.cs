using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CalculateFileHashValues.DAL;
using CalculateFileHashValues.Models;
using CalculateFileHashValues.Services;
using Microsoft.Extensions.Configuration;

namespace CalculateFileHashValues;

internal static class HashSum
{
    private static bool _stopProcess;

    private static async Task Main()
    {
        Console.WriteLine("Enter directory paths separated by a comma");

        while (!_stopProcess)
        {
            var directories = Console.ReadLine();
            var timer = Stopwatch.StartNew();
            Console.WriteLine("Please, standby........");

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static async Task<FileHashItem> CreateFileHashItem(string filePath)
            {
                return new FileHashItem(
                    filePath,
                    BitConverter.ToString(await HashAlgorithms.HashAlgorithms.ComputeSha256(filePath))
                );
            }

            var errorService = new ErrorService();
            var dataTransformer = new DataTransformer<string, Task<FileHashItem>>(CreateFileHashItem, errorService);
            var fileScannerService = new FileScanner(dataTransformer, errorService);
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();

            var dbConnectionString = config.GetConnectionString("SqLite");
            await using var dataCtx = new HashValuesContext(dbConnectionString);
            await using var errorCtx = new HashValuesContext(dbConnectionString);
            dataCtx.ChangeTracker.AutoDetectChangesEnabled = false;
            errorCtx.ChangeTracker.AutoDetectChangesEnabled = false;
            var dbService = new DbService(dataTransformer, errorService, dataCtx, errorCtx);

            var tasks = new List<Task>
            {
                Task.Run(() => fileScannerService.ScanDirectories(directories?.Replace(@"\", @"\\"))),
                Task.Run(dataTransformer.Transform),
                Task.Run(dbService.WriteDataAndErrors)
            };

            await Parallel.ForEachAsync(tasks, async (task, _) => { await task; });

            Console.WriteLine($"Working time: {timer.Elapsed.TotalSeconds} seconds");
            Console.WriteLine("Process finished");
            Console.WriteLine("Close window? 0 - close, 1 - enter new paths");

            switch (Console.ReadLine())
            {
                case "1":
                    break;
                default:
                    _stopProcess = true;
                    break;
            }
        }
    }
}