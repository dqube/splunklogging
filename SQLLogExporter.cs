using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

public class SQLLogExporter : BaseExporter<LogRecord>
{
    private readonly string _connectionString;

    public SQLLogExporter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();

            foreach (var logRecord in batch)
            {
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Logs (Timestamp, LogLevel, Message, Exception)
                    VALUES (@Timestamp, @LogLevel, @Message, @Exception)";

                command.Parameters.AddWithValue("@Timestamp", new DateTimeOffset(logRecord.Timestamp));
                command.Parameters.AddWithValue("@LogLevel", logRecord.LogLevel.ToString());
                command.Parameters.AddWithValue("@Message", logRecord.FormattedMessage);
                command.Parameters.AddWithValue("@Exception", logRecord.Exception?.ToString());

                command.ExecuteNonQuery();
            }
        }

        return ExportResult.Success;
    }

    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        // Perform any necessary cleanup here
        return true;
    }
}