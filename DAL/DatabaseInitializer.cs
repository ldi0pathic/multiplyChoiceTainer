using DbUp;
using DbUp.Engine;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace DAL;

public class DatabaseInitializer(ILoggerFactory loggerFactory)
{
    private readonly ILogger<DatabaseInitializer> _logger = loggerFactory.CreateLogger<DatabaseInitializer>();

    public void Initialize(string? connectionString)
    {
        if (connectionString.IsNullOrEmpty())
        {
            throw new ArgumentNullException(nameof(connectionString));
        }
        
        try
        {
            _logger.LogInformation("Datenbank-Initialisierung gestartet...");

            var scripts = GetMigrationScripts();

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

            _logger.LogInformation("Migration erfolgreich abgeschlossen!");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fehler während der Datenbank-Initialisierung.");
            throw;
        }
    }
    
    private static IEnumerable<SqlScript> GetMigrationScripts()
    {
        return new List<SqlScript>
        {
            new SqlScript("CreateQuestionsTable", @"
            CREATE TABLE IF NOT EXISTS Questions (
                Id TEXT PRIMARY KEY NOT NULL,              -- GUID als Primärschlüssel
                QuestionText TEXT NOT NULL,                -- Text der Frage
                Points INTEGER NOT NULL,                   -- Punkte für die Frage
                LastAskedDate DATETIME,                    -- Datum der letzten Abfrage
                IncorrectAnswerCount INTEGER DEFAULT 0,    -- Anzahl der falschen Antworten
                LastIncorrectAnswerDate DATETIME,          -- Datum der letzten falschen Antwort
                QuestionType TEXT NOT NULL,                -- Fragetyp (z. B. 'Multiple Choice', 'True/False')
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP -- Erstellungsdatum der Frage
            );"),
            
            new SqlScript("CreateAnswersTable", @"
            CREATE TABLE IF NOT EXISTS Answers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,     -- Automatisch inkrementierte ID für die Antwort
                QuestionId TEXT NOT NULL,                  -- Fremdschlüssel auf die Frage
                AnswerText TEXT NOT NULL,                  -- Text der Antwort
                IsCorrect INTEGER NOT NULL,                -- Flag, ob die Antwort korrekt ist (1 = richtig, 0 = falsch)
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP, -- Erstellungsdatum der Antwort
                FOREIGN KEY (QuestionId) REFERENCES Questions(Id) ON DELETE CASCADE -- Verknüpfung zur Frage
            );")
        };
    }
}