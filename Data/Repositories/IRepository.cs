using System.Linq.Expressions;

namespace Berca_Backend.Data.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);

        // Read-only optimized methods
        Task<IEnumerable<T>> GetAllNoTrackingAsync();
        Task<IEnumerable<T>> FindNoTrackingAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultNoTrackingAsync(Expression<Func<T, bool>> predicate);

        // Pagination
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? predicate = null);
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedNoTrackingAsync(int page, int pageSize, Expression<Func<T, bool>>? predicate = null);

        // CRUD Operations
        Task<T> AddAsync(T entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
        Task UpdateAsync(T entity);
        Task UpdateRangeAsync(IEnumerable<T> entities);
        Task DeleteAsync(T entity);
        Task DeleteRangeAsync(IEnumerable<T> entities);
        Task DeleteByIdAsync(int id);

        // Transaction support
        Task<int> SaveChangesAsync();
    }
}