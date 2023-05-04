using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Services;
using CalculateFilesHashCodes.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFilesHashCodes.DAL
{
    public class DbService
    {
        private readonly IDataReader<FileHashItem> _dataTransformer;
        private readonly ErrorService _errorService;
        private readonly HashValuesContext _dataContext;
        private readonly HashValuesContext _errorContext;

        public DbService(
            IDataReader<FileHashItem> dataTranformer,
            ErrorService errorService,
            HashValuesContext dataContext,
            HashValuesContext errorContext)
        {
            _dataTransformer = dataTranformer ?? throw new ArgumentNullException(nameof(dataTranformer));
            _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
            _dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
            _errorContext = errorContext ?? throw new ArgumentNullException(nameof(errorContext));
        }

        public async Task WriteDataAndErrors()
        {
            Console.WriteLine("DbService was started");

            var tasks = new List<Task>
            {
                WriteDataToDb(),
                WriteErrorToDb()
            };

            await Parallel.ForEachAsync(tasks, async (task, _) =>
            {
                await task;
            });

            Console.WriteLine("DbService has finished work");
        }

        private async Task WriteDataToDb()
        {
            while (!_dataTransformer.ErrorReader.Completion.IsCompleted)
            {
                await WriteData();
            }

            await WriteData();

            await _dataContext.SaveChangesAsync();
        }

        private async Task WriteData()
        {
            while (_dataTransformer.ErrorReader.TryRead(out var item))
            {
                await _dataContext.HashItems.AddAsync(item);
            }
        }

        private async Task WriteErrorToDb()
        {
            while (!_errorService.ErrorReader.Completion.IsCompleted)
            {
                await WriteError();
            }

            await WriteError();

            await _errorContext.SaveChangesAsync();

        }

        private async Task WriteError()
        {
            while (_errorService.ErrorReader.TryRead(out var error))
            {
                await _errorContext.Errors.AddAsync(error);
            }
        }
    }
}