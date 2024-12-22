using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DAL;
using Dapper.Contrib.Extensions;

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

    public GenericRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        return await connection.GetAllAsync<T>();
    }

    public async Task<T> GetByIdAsync(int id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        return await connection.GetAsync<T>(id);
    }

    public async Task<long> InsertAsync(T entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        return await connection.InsertAsync(entity);
    }

    public async Task<bool> UpdateAsync(T entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        return await connection.UpdateAsync(entity);
    }

    public async Task<bool> DeleteAsync(T entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        return await connection.DeleteAsync(entity);
    }
}