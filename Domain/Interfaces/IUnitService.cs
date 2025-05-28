using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IUnitService
    {
        Task<List<Unit>> GetAllUnitsAsync();
        Task<Unit> GetUnitByIdAsync(int id);
        Task AddUnitAsync(Unit unit);
        Task UpdateUnitAsync(Unit unit);
        Task DeleteUnitAsync(int id);
        Task<List<Complex>> GetAllComplexesAsync();
        Task<Complex> GetComplexByIdAsync(int id);
        Task AddComplexAsync(Complex complex);
        Task UpdateComplexAsync(Complex complex);
        Task DeleteComplexAsync(int id);
        Task DeleteRateAsync(int rateId);
        Task UpdateRatesAsync(Unit unit, List<Rate> newRates, List<int> rateIdsToDelete);
        DateTime GetFirstOccurrenceInWeek(DayOfWeek dayOfWeek, DateTime startDate);
        DateTime GetLastOccurrenceInWeek(DayOfWeek dayOfWeek, DateTime startDate);
        Task<bool> IsUnitAvailableAsync(int unitId, DateTime checkDate);


    }
}