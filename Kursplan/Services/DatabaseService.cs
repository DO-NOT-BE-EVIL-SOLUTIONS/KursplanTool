using System.Data;
using System.Data.OleDb;

namespace Kursplan.Services;

public class DatabaseService : IDisposable
{
    private OleDbConnection? _connection;
    private OleDbDataAdapter? _adapter;

    public string? FilePath { get; private set; }

    public (bool Success, string ErrorMessage) Connect(string filePath)
    {
        FilePath = filePath;
        
        // This logic is taken from the old Form1, responsible for finding the right OLEDB provider.
        var providers = new[] { "Microsoft.ACE.OLEDB.12.0", "Microsoft.Jet.OLEDB.4.0" };
        var opened = false;
        var lastError = string.Empty;

        foreach (var prov in providers)
        {
            var connStr = $"Provider={prov};Data Source={filePath};Persist Security Info=False;";
            try
            {
                _connection?.Dispose();
                _connection = new OleDbConnection(connStr);
                _connection.Open();
                opened = true;
                break;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                /* try next */
            }
        }

        if (!opened)
        {
            return (false, $"Failed to open Access file. Make sure the Access Database Engine is installed and the file is valid. Last error: {lastError}");
        }

        return (true, string.Empty);
    }

    public (bool AllExist, List<string> MissingTables) ValidateSchema(List<string> requiredTables)
    {
        if (_connection == null) throw new InvalidOperationException("Database is not connected.");

        var schema = _connection.GetSchema("Tables");
        var existingTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (DataRow row in schema.Rows)
        {
            var tableType = row["TABLE_TYPE"]?.ToString();
            var tableName = row["TABLE_NAME"]?.ToString();
            if (string.Equals(tableType, "TABLE", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(tableName))
            {
                if (!tableName.StartsWith("MSys", StringComparison.OrdinalIgnoreCase))
                {
                    existingTables.Add(tableName);
                }
            }
        }

        var missingTables = requiredTables.Where(t => !existingTables.Contains(t)).ToList();
        return (missingTables.Count == 0, missingTables);
    }
    
    public DataTable GetTable(string tableName)
    {
        if (_connection == null) throw new InvalidOperationException("Database is not connected.");

        _adapter?.Dispose();
        _adapter = new OleDbDataAdapter($"SELECT * FROM [{tableName}]", _connection);
        
        _adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
        var dataTable = new DataTable();
        _adapter.Fill(dataTable);
        return dataTable;
    }

    public (bool Success, string ErrorMessage) SaveChanges(DataTable dataTable)
    {
        if (_adapter == null || dataTable == null)
        {
            return (false, "No data has been loaded, so there is nothing to save.");
        }

        try
        {
            using var builder = new OleDbCommandBuilder(_adapter);
            builder.QuotePrefix = "[";
            builder.QuoteSuffix = "]";
            _adapter.Update(dataTable);
            return (true, string.Empty);
        }
        catch (OleDbException ex)
        {
            // This is a rudimentary concurrency check. If the database is locked, an exception will be thrown.
            // ORA-00054: resource busy and acquire with NOWAIT specified or timeout expired
            // The error code for a locked file can vary. A generic catch is safer here.
            return (false, $"Failed to save changes. The database file may be locked by another user. Please try again later. Details: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"An unexpected error occurred while saving changes: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _adapter?.Dispose();
    }
}
