using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading.Tasks;

using CalculateFilesHashCodes.Common;
using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Services;
using CalculateFilesHashCodes.Services.Interfaces;

using Microsoft.Data.Sqlite;

namespace CalculateFilesHashCodes.DAL
{
    public class DbService : IDataService<object>
    {
        private readonly IDataService<FileHashItem> _fileHashService;
        private readonly IDataService<string> _errorService;
        private SqliteConnection _connectionForHashes;
        private SqliteConnection _connectionForErrors;

        public ServiceStatus Status { get; set; }
        public ConcurrentQueue<object> DataQueue { get; }
       
        public DbService(
            IDataService<FileHashItem> fileHashService,
            IDataService<string> errorService)
        {
            _fileHashService = fileHashService ?? throw new ArgumentNullException(nameof(fileHashService));
            _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
        }

        public async Task StartToWriteDataToDb()
        {
            Status = ServiceStatus.Running;
            try
            {
                await CreateAndOpenConnectionDb();
            }
            catch (Exception ex)
            {
                _errorService.DataQueue.Enqueue(ex.Message);
                Console.WriteLine(ex.Message);
                _connectionForHashes.Dispose();
                throw;
            }

            await Task.WhenAll(WriteDataToDb(), WriteErrorToDb());

            Status = ServiceStatus.Completed;
            _connectionForHashes.Dispose();
            Console.WriteLine("DbService has finished work");
        }

        private async Task WriteDataToDb()
        {
            using var cmd = _connectionForHashes.CreateCommand();
            ExecuteCommand(cmd, @"CREATE TABLE IF NOT EXISTS [Hashes] (    
                    [Path] text NOT NULL,
                    [HashValue] text NOT NULL);");

            await _fileHashService.HandlingData(MakeWriteData);
        }

        private void MakeWriteData()
        {
            using var transaction = _connectionForHashes.BeginTransaction();

            while (_fileHashService.DataQueue.TryDequeue(out var item))
            {
                using var cmd = CreateHashInsertCommand();
                ExecuteCommand(cmd, $"INSERT INTO Hashes (Path, HashValue) VALUES ('{item.Path}', '{item.HashValue}')");
            }
            
            transaction.Commit();
        }

        private async Task WriteErrorToDb()
        {
            using var cmd = _connectionForHashes.CreateCommand();
            ExecuteCommand(cmd, @"CREATE TABLE IF NOT EXISTS [Errors] (
                    [Error] text NOT NULL);");

            await _fileHashService.HandlingData(MakeWriteError);
        }

        private void MakeWriteError()
        {
            using var transaction = _connectionForErrors.BeginTransaction();

            while (_errorService.DataQueue.TryDequeue(out var error))
            {
                using var cmd = CreateErrorInsertCommand();
                ExecuteCommand(cmd, $"INSERT INTO Errors (Error) VALUES ('{error}')");
            }

            transaction.Commit();
        }

        private async Task CreateAndOpenConnectionDb()
        {
            var dbName = "Hashes.db";
            var dbPath = Environment.CurrentDirectory + $@"\{dbName}";
            _connectionForHashes = new SqliteConnection($"Data Source={dbPath}");
            _connectionForErrors = new SqliteConnection($"Data Source={dbPath}");
            await _connectionForHashes.OpenAsync();
            await _connectionForErrors.OpenAsync();
        }

        private static void ExecuteCommand(SqliteCommand command, string textCommand)
        {
            command.CommandText = textCommand;
            command.CommandType = CommandType.Text;
            command.ExecuteNonQuery();
        }

        private SqliteCommand CreateErrorInsertCommand() => _connectionForErrors.CreateCommand();

        private SqliteCommand CreateHashInsertCommand() => _connectionForHashes.CreateCommand(); 
    }
}