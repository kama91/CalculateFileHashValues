using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace CalculateFileHashValues.DataAccess;

public sealed class PostgresConnection(string connectionString) : IDisposable
{
    private readonly NpgsqlConnection _connection = new(connectionString);

    public async Task OpenConnection()
    {
        if (_connection == null) return;
        
        await _connection.OpenAsync();
    }

    public async Task ExecuteCommand(string textCommand, IReadOnlyCollection<NpgsqlParameter> parameters)
    {
        if (_connection == null) return;
        
        await using var command = _connection.CreateCommand();
        command.CommandText = textCommand;
        command.CommandType = CommandType.Text;
        command.Parameters.AddRange(parameters.ToArray());
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}