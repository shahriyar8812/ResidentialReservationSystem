using Domain.Entities;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface ICheckInRuleRepository
    {
        Task<CheckInRule> GetByUnitIdAsync(int unitId);
        Task AddAsync(CheckInRule rule);
        Task UpdateAsync(CheckInRule rule);
        Task DeleteAsync(int id);
    }
}