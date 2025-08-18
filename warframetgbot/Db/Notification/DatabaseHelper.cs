using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public static class DatabaseHelper
{
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private static readonly string _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Db", "warframe_bot.db");

    public static SQLiteConnection GetConnection()
    {
        var connection = new SQLiteConnection($"Data Source={_dbPath};Pooling=True;Max Pool Size=100;Default Timeout=30;");
        connection.Open();
        Console.WriteLine($"DatabaseHelper: Connection opened for {_dbPath}");
        return connection;
    }

    public static async Task ExecuteNonQueryAsync(SQLiteConnection connection, string commandText, params (string name, object value)[] parameters)
    {
        await _semaphore.WaitAsync();
        try
        {
            using (var command = new SQLiteCommand(commandText, connection))
            {
                foreach (var (name, value) in parameters)
                {
                    command.Parameters.AddWithValue(name, value);
                }
                Console.WriteLine($"DatabaseHelper: Executing: {commandText} with params {string.Join(", ", parameters.Select(p => $"{p.name}={p.value}"))}");
                await command.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public static async Task<object> ExecuteScalarAsync(SQLiteConnection connection, string commandText, params (string name, object value)[] parameters)
    {
        await _semaphore.WaitAsync();
        try
        {
            using (var command = new SQLiteCommand(commandText, connection))
            {
                foreach (var (name, value) in parameters)
                {
                    command.Parameters.AddWithValue(name, value);
                }
                Console.WriteLine($"DatabaseHelper: Executing: {commandText} with params {string.Join(", ", parameters.Select(p => $"{p.name}={p.value}"))}");
                return await command.ExecuteScalarAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}