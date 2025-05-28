using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Deta;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class FeatureRepository : IFeatureRepository
    {
        private readonly ApplicationDbContext _context;

        public FeatureRepository(ApplicationDbContext context)
        {
            _context = context;
            System.Console.WriteLine("FeatureRepository: Repository initialized.");
        }

        public async Task<List<Feature>> GetAllAsync()
        {
            System.Console.WriteLine("FeatureRepository: Fetching all features...");
            var features = await _context.Features
                .AsNoTracking()
                .ToListAsync();
            System.Console.WriteLine($"FeatureRepository: Retrieved {features.Count} features.");
            return features;
        }
    }
}