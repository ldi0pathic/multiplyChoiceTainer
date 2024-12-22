using DAL;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Logging;

public interface IGenericRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> GetByIdAsync(int id);
    Task<long> InsertAsync(T entity);
    Task<bool> UpdateAsync(T entity);
    Task<bool> DeleteAsync(T entity);
}

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<GenericRepository<T>> _logger;

    public GenericRepository(IDbConnectionFactory dbConnectionFactory, ILoggerFactory loggerFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = loggerFactory.CreateLogger<GenericRepository<T>>();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        try
        {
            return await connection.GetAllAsync<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen aller Datensätze von {TableName}.", typeof(T).Name);
            throw;
        }
    }

    public async Task<T> GetByIdAsync(int id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        try
        {
            return await connection.GetAsync<T>(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen des Datensatzes mit ID {Id} von {TableName}.", id, typeof(T).Name);
            throw;
        }
    }

    public async Task<long> InsertAsync(T entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        try
        {
            return await connection.InsertAsync(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Einfügen eines neuen Datensatzes in {TableName}.", typeof(T).Name);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(T entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        try
        {
            return await connection.UpdateAsync(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Aktualisieren eines Datensatzes in {TableName}.", typeof(T).Name);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(T entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        try
        {
            return await connection.DeleteAsync(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Löschen eines Datensatzes aus {TableName}.", typeof(T).Name);
            throw;
        }
    }
}