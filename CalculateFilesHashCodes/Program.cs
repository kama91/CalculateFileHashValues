using System;
using System.Diagnostics;
using System.Threading.Tasks;

using CalculateFilesHashCodes.Common;
using CalculateFilesHashCodes.DAL;
using CalculateFilesHashCodes.HashCodeAlgorithm;
using CalculateFilesHashCodes.Services;

namespace CalculateFilesHashCodes
{
    internal static class HashSum
    {
        private static bool _stopProcess;

        private static async Task Main()
        {
            while (!_stopProcess)
            {
                Console.WriteLine("Enter a directories paths separated by a comma");
                var directories = Console.ReadLine();
                var timer = Stopwatch.StartNew();
                Console.WriteLine("Please, standby........");
                var errorService = new ErrorService();
                var fileScannerService = new FileScannerService(errorService);
                var fileHashService = new FileHashService(fileScannerService, errorService, new Sha256HashCodeAlgorithm());
                var dbService = new DbService(fileHashService, errorService);
                await Task.WhenAll(
                    fileScannerService.StartScanDirectory(directories?.Replace(@"\", @"\\")),
                    fileHashService.StartCalculate(),
                    dbService.StartToWriteDataToDb());
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
}