using System.Collections.Generic;
using System.Threading.Tasks;

namespace FleetManagement.Data.Abstractions
{
    // (Цей інтерфейс тут для демонстрації, але в нашому завданні
    // ми НЕ будемо його реалізовувати напряму, оскільки ми
    // використовуємо SP, а не T-SQL типу "SELECT * FROM T")
    public interface IGenericRepository<T> where T : class
    {
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
    }
}