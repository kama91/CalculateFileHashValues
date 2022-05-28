using System;
using System.Data;
using Microsoft.Data.Sqlite;
using System.IO;
using CalculateFilesHashCodes.DAL.Interfaces;

namespace CalculateFilesHashCodes.DAL
{
    public class SqLiteDbContext : IDbContext
    {
        private SqliteConnection _connection;

        public void CreateConnectionDb(string dbName)
        {
            var path = Environment.CurrentDirectory + $@"\{dbName}";
            if (!File.Exists(path))
            {
                File.Create(path);
            }

            _connection = new SqliteConnection($"Data Source={dbName}");
        }

        public void OpenConnection()
        {
            _connection?.Open();
        }

        public void ClearConnection()
        {
            _connection?.Dispose();
        }

        public void ExecuteCommand(string textCommand)
        {
            if (_connection == null) return;
            var command = _connection.CreateCommand();
            command.CommandText = textCommand;
            command.CommandType = CommandType.Text;
            command.ExecuteNonQuery();
        }
    }
}
