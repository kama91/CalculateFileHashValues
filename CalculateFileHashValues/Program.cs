using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CalculateFileHashValues.DataAccess;
using CalculateFileHashValues.Models;
using CalculateFileHashValues.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.ObjectPool;

namespace CalculateFileHashValues;

internal static class HashSum
{
    private static bool _stopProcess;

    private static async Task Main()
    {
        while (!_stopProcess)
        {
            Console.WriteLine("Enter directory paths separated by a comma");
            
            var directories = Console.ReadLine();
            var timer = Stopwatch.StartNew();
            Console.WriteLine("Please, standby........");

            var provider = new DefaultObjectPoolProvider();
            var policy = new DefaultPooledObjectPolicy<FileHashItem>();
            var hashItemsPool = provider.Create(policy);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            async Task<FileHashItem> CreateFileHashItem(string filePath)
            {
                var hashItem = hashItemsPool.Get();
                hashItem.Path = filePath;
                hashItem.HashValue = BitConverter.ToString(await HashAlgorithms.HashAlgorithms.ComputeSha256(filePath));

                return hashItem;
            }
            
            var errorService = new ErrorService();
            var dataTransformer = new DataTransformer<string, Task<FileHashItem>>(CreateFileHashItem, errorService);
            var fileScannerService = new FileScanner(dataTransformer, errorService);
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();

            var dbConnectionString = config.GetConnectionString("Postgres");
            await using var dataCtx = new HashValuesContext(dbConnectionString);
            await using var errorCtx = new HashValuesContext(dbConnectionString);
            dataCtx.ChangeTracker.AutoDetectChangesEnabled = false;
            errorCtx.ChangeTracker.AutoDetectChangesEnabled = false;
            var dbService = new DbService(dataTransformer, errorService, dataCtx, errorCtx, hashItemsPool);

            await Task.WhenAll(
                Task.Run(() => fileScannerService.ScanDirectories(directories?.Replace(@"\", @"\\"))),
                Task.Run(dataTransformer.Transform),
                Task.Run(dbService.WriteDataAndErrors));
            
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