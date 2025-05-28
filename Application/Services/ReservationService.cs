using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IUnitService _unitService;

        public ReservationService(
            IReservationRepository reservationRepository,
            IUnitService unitService)
        {
            _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
            _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
            System.Console.WriteLine("ReservationService: Service initialized.");
        }

        public async Task<List<Reservation>> GetAllReservationsAsync()
        {
            System.Console.WriteLine("ReservationService: Fetching all reservations...");
            var reservations = await _reservationRepository.GetAllAsync();
            System.Console.WriteLine($"ReservationService: Retrieved {reservations.Count} reservations.");
            return reservations;
        }

        public async Task<Reservation> GetReservationByIdAsync(int id)
        {
            System.Console.WriteLine($"ReservationService: Fetching reservation with ID {id}...");
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null)
            {
                System.Console.WriteLine($"ReservationService: Reservation with ID {id} not found.");
            }
            else
            {
                System.Console.WriteLine($"ReservationService: Reservation found - UnitId: {reservation.UnitId}, CheckIn: {reservation.CheckInDate}, CheckOut: {reservation.CheckOutDate}, Status: {reservation.Status}");
            }
            return reservation;
        }

        public async Task CreateReservationAsync(Reservation reservation, int unitId)
        {
            System.Console.WriteLine($"ReservationService: Creating reservation for Unit ID {unitId} - CheckIn: {reservation.CheckInDate}, CheckOut: {reservation.CheckOutDate}...");
            reservation.UnitId = unitId;
            reservation.Status = ReservationStatus.Pending;
            reservation.UserId = 2;

            System.Console.WriteLine("ReservationService: Starting date validation...");
            if (!await ValidateDatesAsync(unitId, reservation.CheckInDate, reservation.CheckOutDate))
            {
                System.Console.WriteLine("ReservationService: Date validation failed.");
                throw new Exception("Selected dates are not valid for this unit.");
            }

            System.Console.WriteLine("ReservationService: Checking for overlapping reservations...");
            var existingReservations = await _reservationRepository.GetAllAsync();
            var overlappingReservations = existingReservations
                .Where(r => r.UnitId == unitId &&
                            r.Status != ReservationStatus.Cancelled &&
                            !(reservation.CheckOutDate <= r.CheckInDate || reservation.CheckInDate >= r.CheckOutDate))
                .ToList();

            if (overlappingReservations.Any())
            {
                System.Console.WriteLine($"ReservationService: Overlap found with reservation(s): {string.Join(", ", overlappingReservations.Select(r => r.Id))}");
                throw new Exception("Selected dates overlap with an existing reservation.");
            }

            System.Console.WriteLine("ReservationService: Calculating total price...");
            reservation.TotalPrice = await CalculateTotalPriceAsync(unitId, reservation.CheckInDate, reservation.CheckOutDate);

            reservation.Unit = null;
            System.Console.WriteLine("ReservationService: Saving reservation to database...");
            await _reservationRepository.AddAsync(reservation);
            System.Console.WriteLine($"ReservationService: Reservation created successfully - ID: {reservation.Id}, TotalPrice: {reservation.TotalPrice}.");
        }

        public async Task ConfirmReservationAsync(int id)
        {
            System.Console.WriteLine($"ReservationService: Confirming reservation with ID {id}...");
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null)
            {
                System.Console.WriteLine($"ReservationService: Reservation with ID {id} not found.");
                throw new Exception($"Reservation with ID {id} not found.");
            }
            System.Console.WriteLine($"ReservationService: Current status: {reservation.Status}");
            if (reservation.Status != ReservationStatus.Pending)
            {
                System.Console.WriteLine("ReservationService: Only pending reservations can be confirmed.");
                throw new Exception("Only pending reservations can be confirmed.");
            }
            reservation.Status = ReservationStatus.Confirmed;
            await _reservationRepository.UpdateAsync(reservation);
            System.Console.WriteLine($"ReservationService: Reservation with ID {id} confirmed.");
        }

        public async Task CancelReservationAsync(int id)
        {
            System.Console.WriteLine($"ReservationService: Cancelling reservation with ID {id}...");
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null)
            {
                System.Console.WriteLine($"ReservationService: Reservation with ID {id} not found.");
                throw new Exception($"Reservation with ID {id} not found.");
            }
            reservation.Status = ReservationStatus.Cancelled;
            await _reservationRepository.UpdateAsync(reservation);

            var unit = await _unitService.GetUnitByIdAsync(reservation.UnitId);
            if (unit != null)
            {
                var activeReservations = (await _reservationRepository.GetAllAsync())
                    .Where(r => r.UnitId == reservation.UnitId &&
                                r.Status != ReservationStatus.Cancelled &&
                                r.Id != id)
                    .ToList();

                unit.IsAvailable = !activeReservations.Any();
                await _unitService.UpdateUnitAsync(unit);
                System.Console.WriteLine($"ReservationService: Updated unit availability - IsAvailable: {unit.IsAvailable}");
            }

            System.Console.WriteLine($"ReservationService: Reservation with ID {id} cancelled.");
        }

        public async Task<bool> ValidateDatesAsync(int unitId, DateTime checkInDate, DateTime checkOutDate)
        {
            Console.WriteLine($"ReservationService: Validating dates for Unit ID {unitId}, CheckIn: {checkInDate:yyyy-MM-dd}, CheckOut: {checkOutDate:yyyy-MM-dd}...");

            if (checkInDate >= checkOutDate)
            {
                Console.WriteLine("ReservationService: Check-in date must be before check-out date.");
                return false;
            }

            if (checkInDate < DateTime.Today)
            {
                Console.WriteLine("ReservationService: Check-in date cannot be in the past.");
                return false;
            }

            var unit = await _unitService.GetUnitByIdAsync(unitId);
            if (unit == null)
            {
                Console.WriteLine("ReservationService: Unit not found.");
                return false;
            }

            if (unit.Rates == null || !unit.Rates.Any())
            {
                Console.WriteLine("ReservationService: No rates defined for this unit.");
                return false;
            }

            DateTime currentDate = checkInDate.Date;
            while (currentDate < checkOutDate)
            {
                var applicableRate = unit.Rates.FirstOrDefault(r =>
                    IsDayInRange(currentDate.DayOfWeek.ToString(), r.StartDate.DayOfWeek.ToString(), r.EndDate.DayOfWeek.ToString()));

                if (applicableRate == null)
                {
                    Console.WriteLine($"ReservationService: No valid rate found for date {currentDate:yyyy-MM-dd} (DayOfWeek: {currentDate.DayOfWeek}).");
                    Console.WriteLine($"Available rates: {string.Join(", ", unit.Rates.Select(r => $"[Start: {r.StartDate:yyyy-MM-dd} ({r.StartDate.DayOfWeek}), End: {r.EndDate:yyyy-MM-dd} ({r.EndDate.DayOfWeek}), Price: {r.PricePerNight}]"))}");
                    return false;
                }

                currentDate = currentDate.AddDays(1);
            }

            if (unit.HasMandatoryCheckInOut)
            {
                var checkInDay = checkInDate.DayOfWeek;
                var checkOutDay = checkOutDate.DayOfWeek;

                var applicableRate = unit.Rates.FirstOrDefault(r =>
                    IsDayInRange(checkInDate.DayOfWeek.ToString(), r.StartDate.DayOfWeek.ToString(), r.EndDate.DayOfWeek.ToString()));

                if (applicableRate == null)
                {
                    Console.WriteLine("ReservationService: No applicable rate found for the check-in date.");
                    return false;
                }

                bool isCheckInValid = checkInDay == applicableRate.StartDate.DayOfWeek;
                bool isCheckOutValid = checkOutDay == applicableRate.EndDate.DayOfWeek;

                if (!isCheckInValid || !isCheckOutValid)
                {
                    Console.WriteLine($"ReservationService: Invalid days - CheckIn: {checkInDay}, CheckOut: {checkOutDay}. " +
                        $"Expected Check-in: {applicableRate.StartDate.DayOfWeek}, " +
                        $"Expected Check-out: {applicableRate.EndDate.DayOfWeek}");
                    return false;
                }
            }

            var existingReservations = await _reservationRepository.GetAllAsync();
            bool conflict = existingReservations.Any(r =>
                r.UnitId == unitId &&
                r.Status != ReservationStatus.Cancelled &&
                !(checkOutDate <= r.CheckInDate || checkInDate >= r.CheckOutDate));

            if (conflict)
            {
                Console.WriteLine("ReservationService: Dates conflict with existing reservation.");
                return false;
            }

            Console.WriteLine("ReservationService: Dates are valid.");
            return true;
        }

        public async Task<decimal> CalculateTotalPriceAsync(int unitId, DateTime checkInDate, DateTime checkOutDate)
        {
            System.Console.WriteLine($"ReservationService: Calculating total price for Unit ID {unitId} - CheckIn: {checkInDate:yyyy-MM-dd}, CheckOut: {checkOutDate:yyyy-MM-dd}...");
            var unit = await _unitService.GetUnitByIdAsync(unitId);
            if (unit == null)
            {
                System.Console.WriteLine($"ReservationService: Unit with ID {unitId} not found.");
                throw new Exception($"Unit with ID {unitId} not found.");
            }

            if (unit.Rates == null || !unit.Rates.Any())
            {
                System.Console.WriteLine("ReservationService: No rates defined for this unit.");
                System.Console.WriteLine($"Available rates from UnitService: {string.Join(", ", unit.Rates?.Select(r => $"[ID: {r.Id}, Start: {r.StartDate:yyyy-MM-dd} ({r.StartDate.DayOfWeek}), End: {r.EndDate:yyyy-MM-dd} ({r.EndDate.DayOfWeek}), Price: {r.PricePerNight}]") ?? new List<string>())}");
                throw new Exception("No rates defined for this unit.");
            }

            decimal totalPrice = 0;
            DateTime currentDate = checkInDate.Date;
            System.Console.WriteLine("ReservationService: Calculating price for each day...");
            while (currentDate < checkOutDate)
            {
                System.Console.WriteLine($"ReservationService: Checking rate for date {currentDate:yyyy-MM-dd} (DayOfWeek: {currentDate.DayOfWeek})...");
                var applicableRate = unit.Rates.FirstOrDefault(r =>
                    IsDayInRange(currentDate.DayOfWeek.ToString(), r.StartDate.DayOfWeek.ToString(), r.EndDate.DayOfWeek.ToString()));

                if (applicableRate == null)
                {
                    System.Console.WriteLine($"ReservationService: No valid rate found for day {currentDate:yyyy-MM-dd} (DayOfWeek: {currentDate.DayOfWeek}).");
                    System.Console.WriteLine($"Available rates: {string.Join(", ", unit.Rates.Select(r => $"[ID: {r.Id}, Start: {r.StartDate:yyyy-MM-dd} ({r.StartDate.DayOfWeek}), End: {r.EndDate:yyyy-MM-dd} ({r.EndDate.DayOfWeek}), Price: {r.PricePerNight}]"))}");
                    throw new Exception($"No valid rate found for day {currentDate:yyyy-MM-dd}.");
                }

                totalPrice += applicableRate.PricePerNight;
                System.Console.WriteLine($"ReservationService: Added rate for {currentDate:yyyy-MM-dd} - Rate ID: {applicableRate.Id}, Price: {applicableRate.PricePerNight}, Running Total: {totalPrice}");
                currentDate = currentDate.AddDays(1);
            }

            System.Console.WriteLine($"ReservationService: Total price calculated - Total: {totalPrice} for {checkInDate:yyyy-MM-dd} to {checkOutDate.AddDays(-1):yyyy-MM-dd}.");
            return totalPrice;
        }

        private bool IsDayInRange(string dayToCheck, string startDay, string endDay)
        {
            var days = GetDaysBetween(startDay, endDay).ToList();
            return days.Contains(dayToCheck);
        }

        private IEnumerable<string> GetDaysBetween(string startDay, string endDay)
        {
            var days = new List<string>();
            var start = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), startDay);
            var end = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), endDay);
            var current = start;

            do
            {
                days.Add(current.ToString());
                current = (DayOfWeek)(((int)current + 1) % 7);
            } while (current != end && current != start);

            days.Add(end.ToString());
            return days.Distinct();
        }
    }
}