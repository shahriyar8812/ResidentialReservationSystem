using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Infrastructure.Repositories
{
    public class UnitRepository : IUnitRepository
    {
        private readonly ApplicationDbContext _context;

        public UnitRepository(ApplicationDbContext context)
        {
            _context = context;
            Console.WriteLine("UnitRepository: Initialized successfully.");
        }

        public async Task<List<Unit>> GetAllAsync()
        {
            Console.WriteLine("UnitRepository: Fetching all units...");
            var units = await _context.Units
                .Include(u => u.Complex)
                .Include(u => u.UnitImages)
                .Include(u => u.UnitFeatures).ThenInclude(uf => uf.Feature)
                .Include(u => u.Rates)
                .AsSplitQuery()
                .ToListAsync();
            Console.WriteLine($"UnitRepository: Retrieved {units.Count} units.");
            return units;
        }

        public async Task<Unit> GetByIdAsync(int id)
        {
            Console.WriteLine($"UnitRepository: Fetching unit with ID {id}...");
            var unit = await _context.Units
                .AsNoTracking()
                .Include(u => u.Complex)
                .Include(u => u.UnitImages)
                .Include(u => u.UnitFeatures).ThenInclude(uf => uf.Feature)
                .Include(u => u.Rates)
                .AsSplitQuery()
                .FirstOrDefaultAsync(u => u.Id == id);
            Console.WriteLine(unit == null
                ? $"UnitRepository: Unit with ID {id} not found."
                : $"UnitRepository: Unit found - Title: {unit.Title}");
            return unit;
        }

        public async Task AddAsync(Unit unit)
        {
            Console.WriteLine($"UnitRepository: Adding new unit - Title: {unit.Title}...");
            _context.Units.Add(unit);
            await _context.SaveChangesAsync();
            Console.WriteLine($"UnitRepository: Unit added successfully with ID {unit.Id}.");
        }

        public async Task UpdateAsync(Unit unit)
        {
            Console.WriteLine($"UnitRepository: Updating unit with ID {unit.Id}...");
            var existingUnit = await _context.Units
                .Include(u => u.UnitImages)
                .Include(u => u.UnitFeatures)
                .AsSplitQuery()
                .FirstOrDefaultAsync(u => u.Id == unit.Id);

            if (existingUnit == null)
            {
                Console.WriteLine($"UnitRepository: Unit with ID {unit.Id} not found for update.");
                throw new KeyNotFoundException($"Unit with ID {unit.Id} not found.");
            }

            existingUnit.Title = unit.Title;
            existingUnit.ComplexId = unit.ComplexId;
            existingUnit.Description = unit.Description;
            existingUnit.Capacity = unit.Capacity;
            existingUnit.BedroomCount = unit.BedroomCount;
            existingUnit.IsAvailable = unit.IsAvailable;
            existingUnit.HasMandatoryCheckInOut = unit.HasMandatoryCheckInOut;

            if (unit.UnitImages != null)
            {
                var imagesToDelete = existingUnit.UnitImages
                    .Where(ei => !unit.UnitImages.Any(ui => ui.Id == ei.Id && ui.Id != 0))
                    .ToList();
                if (imagesToDelete.Any())
                {
                    _context.UnitImages.RemoveRange(imagesToDelete);
                    Console.WriteLine($"UnitRepository: Removed {imagesToDelete.Count} images.");
                }

                foreach (var newImage in unit.UnitImages)
                {
                    if (newImage.Id == 0)
                    {
                        newImage.UnitId = unit.Id;
                        existingUnit.UnitImages.Add(newImage);
                        Console.WriteLine($"UnitRepository: Added new image with URL: {newImage.ImageUrl}");
                    }
                    else
                    {
                        var existingImage = existingUnit.UnitImages.FirstOrDefault(i => i.Id == newImage.Id);
                        if (existingImage != null && existingImage.ImageUrl != newImage.ImageUrl)
                        {
                            existingImage.ImageUrl = newImage.ImageUrl;
                            Console.WriteLine($"UnitRepository: Updated image with ID: {newImage.Id}, URL: {newImage.ImageUrl}");
                        }
                    }
                }
                Console.WriteLine($"UnitRepository: Updated UnitImages for unit ID {unit.Id}. New count: {existingUnit.UnitImages.Count}");
            }
            else
            {
                Console.WriteLine($"UnitRepository: No UnitImages provided for unit ID {unit.Id}. Keeping existing images.");
            }

            if (unit.UnitFeatures != null)
            {
                var featuresToDelete = existingUnit.UnitFeatures
                    .Where(ef => !unit.UnitFeatures.Any(uf => uf.FeatureId == ef.FeatureId && uf.UnitId == ef.UnitId))
                    .ToList();
                if (featuresToDelete.Any())
                {
                    _context.UnitFeatures.RemoveRange(featuresToDelete);
                    Console.WriteLine($"UnitRepository: Removed {featuresToDelete.Count} features: {string.Join(", ", featuresToDelete.Select(f => f.FeatureId))}");
                }

                foreach (var newFeature in unit.UnitFeatures)
                {
                    if (!existingUnit.UnitFeatures.Any(ef => ef.UnitId == newFeature.UnitId && ef.FeatureId == newFeature.FeatureId))
                    {
                        newFeature.UnitId = unit.Id;
                        existingUnit.UnitFeatures.Add(newFeature);
                        Console.WriteLine($"UnitRepository: Added new feature with FeatureId: {newFeature.FeatureId} for unit ID {unit.Id}");
                    }
                }
                Console.WriteLine($"UnitRepository: Updated UnitFeatures for unit ID {unit.Id}. New count: {existingUnit.UnitFeatures.Count}");
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"UnitRepository: Unit with ID {unit.Id} updated successfully.");
        }

        public async Task DeleteAsync(int id)
        {
            Console.WriteLine($"UnitRepository: Deleting unit with ID {id}...");
            var unit = await _context.Units.FindAsync(id);
            if (unit == null)
            {
                Console.WriteLine($"UnitRepository: Unit with ID {id} not found for deletion.");
                throw new KeyNotFoundException($"Unit with ID {id} not found.");
            }

            _context.Units.Remove(unit);
            await _context.SaveChangesAsync();
            Console.WriteLine($"UnitRepository: Unit with ID {id} deleted successfully.");
        }

        public async Task<List<Rate>> GetRatesForUnit(int unitId)
        {
            Console.WriteLine($"UnitRepository: Fetching rates for unit ID {unitId}...");
            var rates = await _context.Rates
                .Where(r => r.UnitId == unitId)
                .ToListAsync();
            Console.WriteLine($"UnitRepository: Retrieved {rates.Count} rates for unit ID {unitId}. Rate IDs: {string.Join(", ", rates.Select(r => r.Id))}");
            return rates;
        }

        public async Task AddRateAsync(Rate rate)
        {
            Console.WriteLine($"UnitRepository: Adding new rate for unit ID {rate.UnitId} - StartDate: {rate.StartDate:yyyy-MM-dd}, EndDate: {rate.EndDate:yyyy-MM-dd}, Price: {rate.PricePerNight}");
            rate.Id = 0; 
            _context.Rates.Add(rate);
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"UnitRepository: Rate added successfully with ID {rate.Id}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UnitRepository: Error adding rate - {ex.Message}");
                throw;
            }
        }

        public async Task UpdateRateAsync(Rate rate)
        {
            Console.WriteLine($"UnitRepository: Updating rate with ID {rate.Id}...");
            var existingRate = await _context.Rates.FindAsync(rate.Id);
            if (existingRate == null)
            {
                Console.WriteLine($"UnitRepository: Rate with ID {rate.Id} not found for update.");
                throw new KeyNotFoundException($"Rate with ID {rate.Id} not found.");
            }

            existingRate.StartDate = rate.StartDate;
            existingRate.EndDate = rate.EndDate;
            existingRate.PricePerNight = rate.PricePerNight;
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"UnitRepository: Rate with ID {rate.Id} updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UnitRepository: Error updating rate - {ex.Message}");
                throw;
            }
        }

        public async Task DeleteRateAsync(int rateId)
        {
            Console.WriteLine($"UnitRepository: Deleting rate with ID {rateId}...");
            var rate = await _context.Rates.FindAsync(rateId);
            if (rate == null)
            {
                Console.WriteLine($"UnitRepository: Rate with ID {rateId} not found for deletion.");
                throw new KeyNotFoundException($"Rate with ID {rateId} not found.");
            }

            _context.Rates.Remove(rate);
            await _context.SaveChangesAsync();
            Console.WriteLine($"UnitRepository: Rate with ID {rateId} deleted successfully.");
        }
    }
}