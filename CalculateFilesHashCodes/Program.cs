using System;
using System.Diagnostics;
using CalculateFilesHashCodes.Common;
using CalculateFilesHashCodes.DAL;
using CalculateFilesHashCodes.HashCodeAlgorithm;
using CalculateFilesHashCodes.Services;

namespace CalculateFilesHashCodes
{
    internal static class HashSum
    {
        private static bool _stopProcess;

        private static void Main()
        {
            while (!_stopProcess)
            {
                Console.WriteLine("Enter a directories paths separated by a comma");
                var directories = Console.ReadLine();
                var timer = Stopwatch.StartNew();
                Console.WriteLine("Please, standby........");
                var fileScannerService = new FileScannerService();
                fileScannerService.StartScanDirectory(directories?.Replace(@"\", @"\\"));
                var fileHashService = new FileHashService(fileScannerService, new Md5HashCodeAlgorithm());
                fileHashService.StartCalculate();
                //var dbService = new DbService(fileHashService, new HashSumDbContext());
                var dbService = new DbService(fileHashService, new SqLiteDbContext());
                dbService.StartWriteToDb();
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