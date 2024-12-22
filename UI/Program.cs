using BL;
using DAL;
using DAL.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace multiplyChoiceTainer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        using var loggerFactory = LoggerFactory.Create(builder => { builder.AddSerilog(); });

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // ServiceProvider für DI vorbereiten
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton(loggerFactory)
            .AddScoped<IDbConnectionFactory>(provider => new DbConnectionFactory(connectionString)) // Verbindung übergeben
            .AddScoped<IGenericRepository<Question>, GenericRepository<Question>>()
            .AddScoped<IGenericRepository<Answer>, GenericRepository<Answer>>()
            .AddScoped<IGenericRepository<Migration>, GenericRepository<Migration>>() // Migration Repository hinzufügen
            .AddScoped<QuestionService>()
            .AddScoped<DatabaseInitializer>() // DatabaseInitializer im DI-Container registrieren
            .BuildServiceProvider();

        // DatabaseInitializer mit GenericRepository<Migration> instanziieren und Migrationen anwenden
        var databaseInitializer = serviceProvider.GetRequiredService<DatabaseInitializer>();
        databaseInitializer.Initialize(); // Migrationen anwenden

        // QuestionService und Form1 instanziieren
        var questionService = serviceProvider.GetRequiredService<QuestionService>();
        var form = new Form1(questionService);

        Application.Run(form);
    }
}