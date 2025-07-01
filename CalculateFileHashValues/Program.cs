using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CalculateFileHashValues.DataAccess;
using CalculateFileHashValues.Services;
using Microsoft.Extensions.Configuration;
using Npgsql;

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

            var maxDegreeOfParallelismForScanFile = Environment.ProcessorCount > 3 ? 2 : 1;
            var maxDegreeOfParallelismCalculateHash = Environment.ProcessorCount - maxDegreeOfParallelismForScanFile > 1
                ? Environment.ProcessorCount - maxDegreeOfParallelismForScanFile : 1;

            var hashCalculator = new HashCalculator(streamCleaner,errorService, maxDegreeOfParallelismCalculateHash);
            var fileScannerService = new FileScanner(hashCalculator, errorService, maxDegreeOfParallelismForScanFile);

            await using var dbConnectionHashes = new NpgsqlConnection(dbConnectionString);
            await using var dbConnectionErrors = new NpgsqlConnection(dbConnectionString);
            var dbService = new DbService(hashCalculator, errorService, dbConnectionHashes, dbConnectionErrors);

            await Task.WhenAll(
                fileScannerService.ScanDirectories(directories?.Replace(@"\", @"\\")),
                hashCalculator.Calculate(),
                dbService.Write(),
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