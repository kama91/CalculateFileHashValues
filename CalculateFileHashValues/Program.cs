﻿using CalculateFilesHashCodes.DAL;
using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CalculateFilesHashCodes
{
    internal static class HashSum
    {
        private static bool _stopProcess;

        private static async Task Main()
        {
            Console.WriteLine("Enter a directories paths separated by a comma");

            while (!_stopProcess)
            {
                var directories = Console.ReadLine();
                var timer = Stopwatch.StartNew();
                Console.WriteLine("Please, standby........");

                static FileHashItem CreateFileHashItem(string filePath)
                {
                    return new FileHashItem(
                        filePath,
                        BitConverter.ToString(HashAlgorithms.HashAlgorithms.ComputeSHA256(filePath))
                        );
                }

                var errorService = new ErrorService();
                var dataTransformer = new DataTransformer<string, FileHashItem>(CreateFileHashItem, errorService);
                var fileScannerService = new FileScannerService(dataTransformer, errorService);
                var config = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json").Build();

                var dbConectionString = config.GetConnectionString("SqLite");
                using var dataCtx = new HashValuesContext(dbConectionString);
                using var errorCtx = new HashValuesContext(dbConectionString);
                dataCtx.ChangeTracker.AutoDetectChangesEnabled = false;
                errorCtx.ChangeTracker.AutoDetectChangesEnabled = false;
                var dbService = new DbService(dataTransformer, errorService, dataCtx, errorCtx);

                var tasks = new List<Task>
                {
                    Task.Run(async () => await fileScannerService.ScanDirectories(directories?.Replace(@"\", @"\\"))),
                    Task.Run(dataTransformer.Transform),
                    Task.Run(dbService.WriteDataAndErrors)
                };

                await Parallel.ForEachAsync(tasks, async (task, _) =>
                {
                    await task;
                });

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