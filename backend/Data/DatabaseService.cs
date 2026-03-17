using Npgsql;
namespace Webshop
{

    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            // Read password from environment variable
            var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

            if (string.IsNullOrEmpty(password))
            {
                throw new Exception(
                    "Database password not set. Please set DB_PASSWORD environment variable.");
            }

            _connectionString =
                $"Host=localhost;" +
                $"Port=5001;" +
                $"Username=postgres;" +
                $"Password={password};" +
                $"Database=webshop";
        }

        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}
