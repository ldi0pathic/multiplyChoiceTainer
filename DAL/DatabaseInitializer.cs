using System.Data;
using DAL.Model;
using DbUp;
using DbUp.Engine;
using Microsoft.Extensions.Logging;

namespace DAL;

public class DatabaseInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IGenericRepository<Migration> _migrationRepository;

    public DatabaseInitializer(ILoggerFactory loggerFactory, IDbConnectionFactory connectionFactory, IGenericRepository<Migration> migrationRepository)
    {
        _logger = loggerFactory.CreateLogger<DatabaseInitializer>();
        _connectionFactory = connectionFactory;
        _migrationRepository = migrationRepository;
    }

    public void Initialize()
    {
        var connectionString = _connectionFactory.CreateConnection().ConnectionString;

        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

        try
        {
            _logger.LogInformation("Datenbank-Initialisierung gestartet...");

            ApplyMigrations(connectionString).Wait();

            _logger.LogInformation("Datenbank erfolgreich aktualisiert.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fehler während der Datenbank-Initialisierung.");
            throw;
        }
    }

    private async Task ApplyMigrations(string connectionString)
    {
        using (var connection = _connectionFactory.CreateConnection())
        {
            connection.Open();

            EnsureMigrationTableExists(connection);

            var appliedMigrations = await GetAppliedMigrations();

            var scripts = GetMigrationScripts()
                .Where(s => !appliedMigrations.Contains(s.Name))
                .OrderBy(s => s.Name)
                .ToList();

            if (scripts.Count == 0)
            {
                _logger.LogInformation("Keine neuen Migrationen erforderlich.");
                return;
            }

            var upgrader = DeployChanges.To
                .SqliteDatabase(connectionString)
                .WithScripts(scripts)
                .LogTo(_logger)
                .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                _logger.LogError("Migration fehlgeschlagen: {Error}", result.Error);
                throw result.Error;
            }

            foreach (var script in scripts)
                await _migrationRepository.InsertAsync(new Migration
                {
                    Version = script.Name,
                    AppliedAt = DateTime.Now
                });
        }
    }

    private static void EnsureMigrationTableExists(IDbConnection connection)
    {
        const string tableExistsQuery = "SELECT name FROM sqlite_master WHERE type='table' AND name='Migrations';";
        using (var command = connection.CreateCommand())
        {
            command.CommandText = tableExistsQuery;
            var result = command.ExecuteScalar();
            if (result != null) return;

            const string createTableQuery = @"
                        CREATE TABLE Migrations (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Version TEXT NOT NULL,
                            AppliedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                        );";
            using (var createCmd = connection.CreateCommand())
            {
                createCmd.CommandText = createTableQuery;
                createCmd.ExecuteNonQuery();
            }
        }
    }

    private async Task<List<string>> GetAppliedMigrations()
    {
        var migrations = await _migrationRepository.GetAllAsync();
        return migrations.Select(m => m.Version).ToList();
    }

    private static IEnumerable<SqlScript> GetMigrationScripts()
    {
        return new List<SqlScript>
        {
            new("241222_01_CreateQuestionsTable", @"
                CREATE TABLE IF NOT EXISTS Questions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                    QuestionText TEXT NOT NULL,
                    Points INTEGER NOT NULL,
                    LastAskedDate DATETIME,
                    IncorrectAnswerCount INTEGER DEFAULT 0,
                    LastIncorrectAnswerDate DATETIME,
                    QuestionType INTEGER NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );"),

            new("241222_02_CreateAnswersTable", @"
                CREATE TABLE IF NOT EXISTS Answers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                    QuestionId INTEGER NOT NULL,
                    AnswerText TEXT NOT NULL,
                    IsCorrect INTEGER NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (QuestionId) REFERENCES Questions(Id) ON DELETE CASCADE
                );"),

            new("241222_03_AddIsDeletedColumnToQuestions", @"
                        ALTER TABLE Questions ADD COLUMN IsDeleted INTEGER DEFAULT 0;
                    "),

            new("241226_01_AddHeadersToQuestions", @"
                        ALTER TABLE Questions ADD COLUMN Header1 TEXT NULL
                    "),
            new("241226_02_AddHeadersToQuestions", @"
                        ALTER TABLE Questions ADD COLUMN Header2 TEXT NULL
                    "),
            new("250104_reorderAnswer", @"
                        ALTER TABLE Answers ADD COLUMN CanReorder INTEGER DEFAULT 0;
                    ")
        };
    }
}