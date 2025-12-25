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
            
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();

            var dbConnectionString = config.GetConnectionString("Postgres");

            var errorService = new ErrorService();
            var fileScannerService = new FileScanner(errorService);
            var hashCalculator = new HashCalculator(fileScannerService, errorService);

            await using var dbConnectionHashes = new NpgsqlConnection(dbConnectionString);
            await using var dbConnectionErrors = new NpgsqlConnection(dbConnectionString);
            var dbService = new DbService(hashCalculator, errorService, dbConnectionHashes, dbConnectionErrors);

            await Task.WhenAll(
                fileScannerService.CollectFilePaths(directories?.Replace(@"\", @"\\")),
                hashCalculator.Calculate(),
                dbService.Write());
            
            Console.WriteLine($"Working time: {timer.Elapsed.TotalSeconds} seconds");
            Console.WriteLine("Close window? y - close, any key for continue");

            _stopProcess = Console.ReadLine() switch
            {
                "y" => true,
                _ => false
            };
        }
    }
}