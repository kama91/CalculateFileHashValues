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
        private SqliteConnection _connectionForHashes;
        private SqliteConnection _errorsConnection;

        public DbService(
            IDataReader<FileHashItem> dataTranformer,
            ErrorService errorService)
        {
            _dataTransformer = dataTranformer ?? throw new ArgumentNullException(nameof(dataTranformer));
            _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
        }

        public async Task StartToWriteDataToDb()
        {
            try
            {
                await CreateAndOpenConnectionDb();
            }
            catch (Exception ex)
            {                
                Console.WriteLine(ex.Message);
                
                _connectionForHashes.Dispose();
                
                throw;
            }

            Parallel.Invoke(WriteDataToDb, WriteErrorToDb);

            _connectionForHashes.Dispose();

            Console.WriteLine("DbService has finished work");
        }

        private void WriteDataToDb()
        {
            using var createTableCmd = _connectionForHashes.CreateCommand();
            
            ExecuteCommand(createTableCmd, @"CREATE TABLE IF NOT EXISTS [Hashes] (    
                        [Path] text NOT NULL,
                        [HashValue] text NOT NULL);");

            using var transaction = _connectionForHashes.BeginTransaction();

            while (!_dataTransformer.DataReader.Completion.IsCompleted &&
                _dataTransformer.DataReader.TryRead(out var item))
            {
                using var insertCmd = _connectionForHashes.CreateCommand();
                ExecuteCommand(insertCmd, $"INSERT INTO Hashes VALUES ('{item.Path}', '{item.HashValue}')");
            }

            transaction.Commit();

            _errorService.DataWriter.Complete();
        }

        private void WriteErrorToDb()
        {
            using var createTableCmd = _errorsConnection.CreateCommand();

            ExecuteCommand(createTableCmd, @"CREATE TABLE IF NOT EXISTS [Errors] (
                    [Error] text NOT NULL);");

            using var transaction = _errorsConnection.BeginTransaction();

            while (!_errorService.DataReader.Completion.IsCompleted &&
                _errorService.DataReader.TryRead(out var error))
            {
                using var insertCmd = _errorsConnection.CreateCommand();
                ExecuteCommand(insertCmd, $"INSERT INTO Errors VALUES ('{error.Replace("'", string.Empty)}')");
            }

            transaction.Commit();
        }

        private async Task CreateAndOpenConnectionDb()
        {
            var dbName = "Hashes.db";
            var dbPath = Environment.CurrentDirectory + $@"\{dbName}";
            
            _connectionForHashes = new SqliteConnection($"Data Source={dbPath}");
            _errorsConnection = new SqliteConnection($"Data Source={dbPath}");
            
            await _connectionForHashes.OpenAsync();
            await _errorsConnection.OpenAsync();
        }

        private static void ExecuteCommand(SqliteCommand command, string textCommand)
        {
            command.CommandText = textCommand;
            command.CommandType = CommandType.Text;
            command.ExecuteNonQuery();
        }
    }
}