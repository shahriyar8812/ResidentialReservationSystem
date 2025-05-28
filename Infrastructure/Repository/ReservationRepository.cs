using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Deta;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly ApplicationDbContext _context;

        public ReservationRepository(ApplicationDbContext context)
        {
            _context = context;
            System.Console.WriteLine("ReservationRepository: Repository initialized.");
        }

        public async Task<List<Reservation>> GetAllAsync()
        {
            System.Console.WriteLine("ReservationRepository: Fetching all reservations...");
            var reservations = await _context.Reservations
                .Include(r => r.Unit)
                    .ThenInclude(u => u.Rates)
                .Include(r => r.User)
                .AsNoTracking()
                .ToListAsync();
            System.Console.WriteLine($"ReservationRepository: Retrieved {reservations.Count} reservations.");
            return reservations;
        }

        public async Task<Reservation> GetByIdAsync(int id)
        {
            System.Console.WriteLine($"ReservationRepository: Fetching reservation with ID {id}...");
            var reservation = await _context.Reservations
                .Include(r => r.Unit)
                    .ThenInclude(u => u.Rates)
                .Include(r => r.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
            if (reservation == null)
            {
                System.Console.WriteLine($"ReservationRepository: Reservation with ID {id} not found.");
            }
            else
            {
                System.Console.WriteLine($"ReservationRepository: Reservation found - ID: {reservation.Id}");
            }
            return reservation;
        }

        public async Task AddAsync(Reservation reservation)
        {
            System.Console.WriteLine($"ReservationRepository: Adding new reservation...");
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            System.Console.WriteLine($"ReservationRepository: Reservation added successfully with ID {reservation.Id}.");
        }

        public async Task UpdateAsync(Reservation reservation)
        {
            System.Console.WriteLine($"ReservationRepository: Updating reservation with ID {reservation.Id}...");
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();
            System.Console.WriteLine($"ReservationRepository: Reservation with ID {reservation.Id} updated successfully.");
        }

        public async Task DeleteAsync(int id)
        {
            System.Console.WriteLine($"ReservationRepository: Deleting reservation with ID {id}...");
            var reservation = await GetByIdAsync(id);
            if (reservation != null)
            {
                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();
                System.Console.WriteLine($"ReservationRepository: Reservation with ID {id} deleted successfully.");
            }
            else
            {
                System.Console.WriteLine($"ReservationRepository: Reservation with ID {id} not found for deletion.");
            }
        }
    }
}