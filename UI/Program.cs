using DAL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace multiplyChoiceTainer;

internal static class Program
{
    [STAThread]
    static void Main()
    { 
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) 
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration) 
            .CreateLogger();
        
 
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog();
        });
        
        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        
        var databaseInitializer = new DatabaseInitializer(loggerFactory);
        databaseInitializer.Initialize(connectionString);
            
 
        Application.Run(new Form1());
    }
}