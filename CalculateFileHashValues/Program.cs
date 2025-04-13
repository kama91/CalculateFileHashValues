using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CalculateFileHashValues.DataAccess;
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

            var errorService = new ErrorService();
            var streamCleaner = new StreamCleaner();
            
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();

            var dbConnectionString = config.GetConnectionString("Postgres");

            var dataTransformer = new DataTransformer(streamCleaner,errorService);
            var fileScannerService = new FileScanner(dataTransformer, errorService);
            
            using var dbConnection = new PostgresConnection(dbConnectionString);
            var dbService = new DbService(dataTransformer, errorService, dbConnection);
            
            await Task.WhenAll(
                fileScannerService.ScanDirectories(directories?.Replace(@"\", @"\\")),
                dataTransformer.Transform(),
                dbService.WriteDataAndErrors(),
                streamCleaner.Clean());
            
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