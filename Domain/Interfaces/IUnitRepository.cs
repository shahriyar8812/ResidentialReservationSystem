using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IUnitRepository
    {
        Task<List<Unit>> GetAllAsync();
        Task<Unit> GetByIdAsync(int id);
        Task AddAsync(Unit unit);
        Task UpdateAsync(Unit unit);
        Task DeleteAsync(int id);
        Task<List<Rate>> GetRatesForUnit(int unitId);
        Task AddRateAsync(Rate rate);
        Task UpdateRateAsync(Rate rate);
        Task DeleteRateAsync(int rateId);
    }
}