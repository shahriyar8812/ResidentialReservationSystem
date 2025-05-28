using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IReservationService
    {
        Task<List<Reservation>> GetAllReservationsAsync();
        Task<Reservation> GetReservationByIdAsync(int id);
        Task CreateReservationAsync(Reservation reservation, int unitId);
        Task ConfirmReservationAsync(int id);
        Task CancelReservationAsync(int id);
        Task<bool> ValidateDatesAsync(int unitId, DateTime checkInDate, DateTime checkOutDate);
        Task<decimal> CalculateTotalPriceAsync(int unitId, DateTime checkInDate, DateTime checkOutDate);
    }
}