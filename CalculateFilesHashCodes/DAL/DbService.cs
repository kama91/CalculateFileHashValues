using System;
using System.Data;
using System.Threading.Tasks;

using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Services;
using CalculateFilesHashCodes.Services.Interfaces;

using Microsoft.Data.Sqlite;

namespace CalculateFilesHashCodes.DAL
{
    public class DbService
    {
        private readonly IDataReader<FileHashItem> _dataTransformer;
        private readonly ErrorService _errorService;
        private SqliteConnection _hashesConnection;
        private SqliteConnection _errorsConnection;

        public DbService(
            IDataReader<FileHashItem> dataTranformer,
            ErrorService errorService)
        {
            _dataTransformer = dataTranformer ?? throw new ArgumentNullException(nameof(dataTranformer));
            _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
        }

        public async Task WriteDataAndErrorsAsync()
        {
            try
            {
                await CreateAndOpenConnectionsDb();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                await ClearConnections();

                throw;
            }

            await Task.WhenAll(WriteDataToDb(), WriteErrorToDb());

            await ClearConnections();

            Console.WriteLine("DbService has finished work");
        }

        private async Task ClearConnections()
        {
            if (_hashesConnection != null)
            {
                await _hashesConnection.DisposeAsync();
            }

            if (_errorsConnection != null)
            {
                await _errorsConnection.DisposeAsync();
            }
        }

        private async Task WriteDataToDb()
        {
            using var createTableCmd = _hashesConnection.CreateCommand();

            await ExecuteCommand(createTableCmd, @"CREATE TABLE IF NOT EXISTS [Hashes] (    
                        [Path] text NOT NULL,
                        [HashValue] text NOT NULL);");

            using var transaction = await _hashesConnection.BeginTransactionAsync();

            while (!_dataTransformer.DataReader.Completion.IsCompleted &&
                _dataTransformer.DataReader.TryRead(out var item))
            {
                using var insertCmd = _hashesConnection.CreateCommand();
                await ExecuteCommand(insertCmd, $"INSERT INTO Hashes VALUES ('{item.Path}', '{item.HashValue}')");
            }

            await transaction.CommitAsync();

            _errorService.DataWriter.Complete();
        }

        private async Task WriteErrorToDb()
        {
            using var createTableCmd = _errorsConnection.CreateCommand();

            await ExecuteCommand(createTableCmd, @"CREATE TABLE IF NOT EXISTS [Errors] (
                    [Error] text NOT NULL);");

            using var transaction = await _errorsConnection.BeginTransactionAsync();

            var errorCounter = 0;

            while (!_errorService.DataReader.Completion.IsCompleted &&
                _errorService.DataReader.TryRead(out var error))
            {
                using var insertCmd = _errorsConnection.CreateCommand();
                await ExecuteCommand(insertCmd, $"INSERT INTO Errors VALUES ('{error.Replace("'", string.Empty)}')");
                ++errorCounter;
            }

            if (errorCounter == 0)
            {
                await transaction.DisposeAsync();
                using var dropTableCmd = _errorsConnection.CreateCommand();
                await ExecuteCommand(dropTableCmd, @"DROP TABLE ERRORS"); 
            }
            else
            {
                await transaction.CommitAsync();
            }
        }

        private async Task CreateAndOpenConnectionsDb()
        {
            var dbName = "Hashes.db";
            var dbPath = Environment.CurrentDirectory + $@"\{dbName}";
            
            _hashesConnection = new SqliteConnection($"Data Source={dbPath}");
            _errorsConnection = new SqliteConnection($"Data Source={dbPath}");
            
            await _hashesConnection.OpenAsync();
            await _errorsConnection.OpenAsync();
        }

        private static async Task ExecuteCommand(SqliteCommand command, string textCommand)
        {
            command.CommandText = textCommand;
            command.CommandType = CommandType.Text;
            await command.ExecuteNonQueryAsync();
        }
    }
}