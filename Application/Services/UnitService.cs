using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UnitService : IUnitService
    {
        private readonly IUnitRepository _unitRepository;
        private readonly IComplexRepository _complexRepository;
        private readonly IReservationRepository _reservationRepository;

        public UnitService(
            IUnitRepository unitRepository,
            IComplexRepository complexRepository,
            IReservationRepository reservationRepository)
        {
            _unitRepository = unitRepository ?? throw new ArgumentNullException(nameof(unitRepository));
            _complexRepository = complexRepository ?? throw new ArgumentNullException(nameof(complexRepository));
            _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
            Console.WriteLine("UnitService: Initialized successfully.");
        }

        public async Task<bool> IsUnitAvailableAsync(int unitId, DateTime checkDate)
        {
            Console.WriteLine($"UnitService: Checking availability for Unit ID {unitId} on {checkDate:yyyy-MM-dd}...");
            var reservations = await _reservationRepository.GetAllAsync();
            var activeReservations = reservations
                .Where(r => r.UnitId == unitId &&
                            r.Status != ReservationStatus.Cancelled &&
                            checkDate >= r.CheckInDate && checkDate < r.CheckOutDate)
                .ToList();

            bool isAvailable = !activeReservations.Any();
            Console.WriteLine($"UnitService: Unit ID {unitId} is {(isAvailable ? "available" : "not available")} on {checkDate:yyyy-MM-dd}.");
            return isAvailable;
        }

        public async Task<List<Unit>> GetAllUnitsAsync()
        {
            Console.WriteLine("UnitService: Fetching all units...");
            var units = await _unitRepository.GetAllAsync();
            foreach (var unit in units)
            {
                unit.IsAvailable = await IsUnitAvailableAsync(unit.Id, DateTime.Today);
                if (unit.Rates == null || !unit.Rates.Any())
                {
                    Console.WriteLine($"UnitService: Rates for Unit ID {unit.Id} are empty or null. Fetching manually...");
                    unit.Rates = await _unitRepository.GetRatesForUnit(unit.Id);
                }
            }
            Console.WriteLine($"UnitService: Retrieved {units.Count} units.");
            return units;
        }

        public async Task<Unit> GetUnitByIdAsync(int id)
        {
            Console.WriteLine($"UnitService: Fetching unit with ID {id}...");
            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit != null)
            {
                if (unit.Rates == null || !unit.Rates.Any())
                {
                    Console.WriteLine("UnitService: Rates are empty or null. Fetching manually...");
                    unit.Rates = await _unitRepository.GetRatesForUnit(id);
                }
                unit.IsAvailable = await IsUnitAvailableAsync(id, DateTime.Today);
                Console.WriteLine($"UnitService: Unit found - Title: {unit.Title}, IsAvailable: {unit.IsAvailable}, Rates Count: {unit.Rates?.Count ?? 0}");
            }
            else
            {
                Console.WriteLine($"UnitService: Unit with ID {id} not found.");
            }
            return unit;
        }

        public async Task AddUnitAsync(Unit unit)
        {
            Console.WriteLine($"UnitService: Adding new unit - Title: {unit.Title}...");
            var rates = unit.Rates?.ToList() ?? new List<Rate>();

            await _unitRepository.AddAsync(unit);
            Console.WriteLine($"UnitService: Unit added successfully with ID {unit.Id}.");

            if (rates.Any())
            {
                Console.WriteLine($"UnitService: Adding {rates.Count} rates for Unit ID {unit.Id}...");
                foreach (var rate in rates)
                {
                    rate.UnitId = unit.Id;
                    await _unitRepository.AddRateAsync(rate);
                    Console.WriteLine($"UnitService: Added rate - StartDate: {rate.StartDate:yyyy-MM-dd}, EndDate: {rate.EndDate:yyyy-MM-dd}, Price: {rate.PricePerNight}");
                }
            }
            else
            {
                Console.WriteLine($"UnitService: No rates provided for Unit ID {unit.Id}.");
            }
        }

        public async Task UpdateUnitAsync(Unit unit)
        {
            Console.WriteLine($"UnitService: Updating unit with ID {unit.Id}...");
            await _unitRepository.UpdateAsync(unit);
            Console.WriteLine($"UnitService: Unit with ID {unit.Id} updated successfully.");
        }

        public async Task DeleteUnitAsync(int id)
        {
            Console.WriteLine($"UnitService: Deleting unit with ID {id}...");
            await _unitRepository.DeleteAsync(id);
            Console.WriteLine($"UnitService: Unit with ID {id} deleted successfully.");
        }

        public async Task<List<Complex>> GetAllComplexesAsync()
        {
            Console.WriteLine("UnitService: Fetching all complexes...");
            var complexes = await _complexRepository.GetAllAsync();
            Console.WriteLine($"UnitService: Retrieved {complexes.Count} complexes.");
            return complexes;
        }

        public async Task<Complex> GetComplexByIdAsync(int id)
        {
            Console.WriteLine($"UnitService: Fetching complex with ID {id}...");
            var complex = await _complexRepository.GetByIdAsync(id);
            if (complex == null)
                Console.WriteLine($"UnitService: Complex with ID {id} not found.");
            else
                Console.WriteLine($"UnitService: Complex found - Name: {complex.Name}");
            return complex;
        }

        public async Task AddComplexAsync(Complex complex)
        {
            Console.WriteLine($"UnitService: Adding new complex: {complex.Name}...");
            await _complexRepository.AddAsync(complex);
            Console.WriteLine($"UnitService: Complex {complex.Name} added successfully.");
        }

        public async Task UpdateComplexAsync(Complex complex)
        {
            Console.WriteLine($"UnitService: Updating complex with ID {complex.Id}...");
            await _complexRepository.UpdateAsync(complex);
            Console.WriteLine($"UnitService: Complex with ID {complex.Id} updated successfully.");
        }

        public async Task DeleteComplexAsync(int id)
        {
            Console.WriteLine($"UnitService: Deleting complex with ID {id}...");
            var units = await _unitRepository.GetAllAsync();
            if (units.Any(u => u.ComplexId == id))
            {
                Console.WriteLine($"UnitService: Cannot delete complex with ID {id} because it has associated units.");
                throw new InvalidOperationException("Cannot delete complex because it has associated units.");
            }
            await _complexRepository.DeleteAsync(id);
            Console.WriteLine($"UnitService: Complex with ID {id} deleted successfully.");
        }

        public async Task DeleteRateAsync(int rateId)
        {
            Console.WriteLine($"UnitService: Deleting rate with ID {rateId}...");
            await _unitRepository.DeleteRateAsync(rateId);
            Console.WriteLine($"UnitService: Rate with ID {rateId} deleted successfully.");
        }

        public async Task UpdateRatesAsync(Unit unit, List<Rate> newRates, List<int> deleteRateIds)
        {
            try
            {
                Console.WriteLine($"UnitService: Updating rates for unit ID {unit.Id}...");
                var existingRates = await _unitRepository.GetRatesForUnit(unit.Id);
                Console.WriteLine($"UnitService: Existing rates: {string.Join(", ", existingRates.Select(r => $"[ID: {r.Id}, StartDate: {r.StartDate:yyyy-MM-dd}, EndDate: {r.EndDate:yyyy-MM-dd}, Price: {r.PricePerNight}]"))}");

                foreach (var rate in existingRates)
                {
                    if (deleteRateIds.Contains(rate.Id))
                    {
                        await _unitRepository.DeleteRateAsync(rate.Id);
                        Console.WriteLine($"UnitService: Deleted rate with ID {rate.Id}");
                    }
                }

                foreach (var newRate in newRates)
                {
                    Console.WriteLine($"UnitService: Processing new rate: [StartDate: {newRate.StartDate:yyyy-MM-dd}, EndDate: {newRate.EndDate:yyyy-MM-dd}, Price: {newRate.PricePerNight}, ID: {newRate.Id}]");

                    if (newRate.StartDate > newRate.EndDate)
                    {
                        Console.WriteLine($"UnitService: Invalid rate skipped - StartDate {newRate.StartDate:yyyy-MM-dd} > EndDate {newRate.EndDate:yyyy-MM-dd}");
                        continue;
                    }

                    newRate.UnitId = unit.Id;

                    var matchedExisting = existingRates.FirstOrDefault(r =>
                        r.Id == newRate.Id &&
                        !deleteRateIds.Contains(r.Id)
                    );

                    if (matchedExisting != null)
                    {
                        if (matchedExisting.StartDate != newRate.StartDate ||
                            matchedExisting.EndDate != newRate.EndDate ||
                            matchedExisting.PricePerNight != newRate.PricePerNight)
                        {
                            matchedExisting.StartDate = newRate.StartDate;
                            matchedExisting.EndDate = newRate.EndDate;
                            matchedExisting.PricePerNight = newRate.PricePerNight;
                            await _unitRepository.UpdateRateAsync(matchedExisting);
                            Console.WriteLine($"UnitService: Updated rate with ID {matchedExisting.Id}");
                        }
                        else
                        {
                            Console.WriteLine($"UnitService: No changes detected for rate ID {matchedExisting.Id}, skipping...");
                        }
                        continue;
                    }

                    await _unitRepository.AddRateAsync(newRate);
                    Console.WriteLine($"UnitService: Added new rate - StartDate: {newRate.StartDate:yyyy-MM-dd}, EndDate: {newRate.EndDate:yyyy-MM-dd}, Price: {newRate.PricePerNight}");
                }

                Console.WriteLine("UnitService: Rates updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UnitService: Error in UpdateRatesAsync: {ex.Message}");
                throw;
            }
        }

        public DateTime GetFirstOccurrenceInWeek(DayOfWeek targetDay, DateTime rangeStart)
        {
            var referenceDate = rangeStart.Date;
            int daysUntilTarget = ((int)targetDay - (int)referenceDate.DayOfWeek + 7) % 7;
            var result = referenceDate.AddDays(daysUntilTarget).Date;
            if (result < rangeStart)
            {
                result = result.AddDays(7);
            }
            Console.WriteLine($"UnitService: GetFirstOccurrenceInWeek - Reference: {rangeStart:yyyy-MM-dd}, Target: {targetDay}, Result: {result:yyyy-MM-dd}");
            return result;
        }

        public DateTime GetLastOccurrenceInWeek(DayOfWeek endDay, DateTime startDate)
        {
            int startDay = (int)startDate.DayOfWeek;
            int targetDay = (int)endDay;

            int daysUntilTarget = ((targetDay - startDay + 7) % 7);
            var result = startDate.AddDays(daysUntilTarget).Date;

            Console.WriteLine($"UnitService: GetLastOccurrenceInWeek - Start: {startDate:yyyy-MM-dd}, EndDay: {endDay}, Result: {result:yyyy-MM-dd}");
            return result;
        }



    }
}