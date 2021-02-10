using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using CalculateFilesHashCodes.DAL.Interfaces;

namespace CalculateFilesHashCodes.DAL
{
    public class SqLiteDbContext : IDbContext
    {
        private SQLiteConnection _connection;

        public void CreateConnectionDb(string dbName)
        {
            var path = Environment.CurrentDirectory + $@"\{dbName}";
            if (!File.Exists(path))
            {
                SQLiteConnection.CreateFile(path);
            }

            _connection = new SQLiteConnection($"Data Source={dbName};Version=3");
        }

        public void OpenConnection()
        {
            _connection?.Open();
        }

        public void ClearConnection()
        {
            _connection?.Dispose();
        }

        public void ExecuteQuery(string textCommand)
        {
            if (_connection == null) return;
            var command = _connection.CreateCommand();
            command.CommandText = textCommand;
            command.CommandType = CommandType.Text;
            command.ExecuteNonQuery();
        }
    }
}
