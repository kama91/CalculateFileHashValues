using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CalculateFileHashValues.DataAccess;
using CalculateFileHashValues.Models;
using CalculateFileHashValues.Services;
using Microsoft.Extensions.Configuration;

namespace CalculateFileHashValues;

internal static class HashSum
{
    private static bool _stopProcess;

    private static async Task Main()
    {
        while (!_stopProcess)
        {
            Console.WriteLine("Enter directory paths separated by comma");
            
            var directories = Console.ReadLine();
            var timer = Stopwatch.StartNew();
            Console.WriteLine("Please, standby........");

            FileHashItem CreateFileHashItem(string filePath)
            {
                return new FileHashItem
                {
                    Path = new ReadOnlyMemory<char>(filePath.ToCharArray()) ,
                    HashValue = HashAlgorithms.HashAlgorithms.ComputeSha256Sync(filePath)
                };
            }
            
            var errorService = new ErrorService();
            var dataTransformer = new DataTransformer<string, FileHashItem>(CreateFileHashItem, errorService);
            var fileScannerService = new FileScanner(dataTransformer, errorService);
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();

            var dbConnectionString = config.GetConnectionString("Postgres");
            await using var dataCtx = new HashValuesContext(dbConnectionString);
            await using var errorCtx = new HashValuesContext(dbConnectionString);
            dataCtx.ChangeTracker.AutoDetectChangesEnabled = false;
            errorCtx.ChangeTracker.AutoDetectChangesEnabled = false;
            var dbService = new DbService(dataTransformer, errorService, dataCtx, errorCtx);

            await Task.WhenAll(
                Task.Run(() => fileScannerService.ScanDirectories(directories?.Replace(@"\", @"\\"))),
                Task.Run(dataTransformer.Transform),
                Task.Run(dbService.WriteDataAndErrors));
            
            Console.WriteLine($"Working time: {timer.Elapsed.TotalSeconds} seconds");
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