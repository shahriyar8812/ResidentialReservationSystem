using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IComplexRepository
    {
        Task<List<Complex>> GetAllAsync();
        Task<Complex> GetByIdAsync(int id);
        Task AddAsync(Complex complex);
        Task UpdateAsync(Complex complex);
        Task DeleteAsync(int id);
    }
}