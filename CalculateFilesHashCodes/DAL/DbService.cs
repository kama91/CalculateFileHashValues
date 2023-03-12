using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Services;
using CalculateFilesHashCodes.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace CalculateFilesHashCodes.DAL
{
    public class DbService
    {
        private readonly IDataReader<FileHashItem> _dataTransformer;
        private readonly ErrorService _errorService;
        private readonly FileContext _fileContext;

        public DbService(
            IDataReader<FileHashItem> dataTranformer,
            ErrorService errorService,
            FileContext fileContext)
        {
            _dataTransformer = dataTranformer ?? throw new ArgumentNullException(nameof(dataTranformer));
            _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
            _fileContext = fileContext ?? throw new ArgumentNullException(nameof(fileContext));
        }

        public async Task WriteDataAndErrorsAsync()
        {
            Console.WriteLine("DbService was started");

            await Task.WhenAll(WriteDataToDb(), WriteErrorToDb());

            Console.WriteLine("DbService has finished work");
        }

        private async Task WriteDataToDb()
        {
            while (!_dataTransformer.DataReader.Completion.IsCompleted &&
                _dataTransformer.DataReader.TryRead(out var item))
            {
                await _fileContext.HashItems.AddAsync(item);
            }

            await _fileContext.SaveChangesAsync();
        }

        private async Task WriteErrorToDb()
        {

            while (!_errorService.DataReader.Completion.IsCompleted &&
                _errorService.DataReader.TryRead(out var error))
            {
                await _fileContext.Errors.AddAsync(error);
            }

            await _fileContext.SaveChangesAsync();

        }
    }
}