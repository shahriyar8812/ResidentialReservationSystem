using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Deta.Repositories
{
    public class ComplexRepository : IComplexRepository
    {
        private readonly ApplicationDbContext _context;

        public ComplexRepository(ApplicationDbContext context)
        {
            _context = context;
            System.Console.WriteLine("ComplexRepository: Initialized successfully.");
        }

        public async Task<List<Complex>> GetAllAsync()
        {
            System.Console.WriteLine("ComplexRepository: Fetching all complexes...");
            var complexes = await _context.Complexes.ToListAsync();
            System.Console.WriteLine($"ComplexRepository: Retrieved {complexes.Count} complexes.");
            return complexes;
        }

        public async Task<Complex> GetByIdAsync(int id)
        {
            System.Console.WriteLine($"ComplexRepository: Fetching complex with ID {id}...");
            var complex = await _context.Complexes.FindAsync(id);
            if (complex == null)
            {
                System.Console.WriteLine($"ComplexRepository: Complex with ID {id} not found.");
            }
            else
            {
                System.Console.WriteLine($"ComplexRepository: Found complex with ID {id}, Name: {complex.Name}.");
            }
            return complex;
        }

        public async Task AddAsync(Complex complex)
        {
            System.Console.WriteLine($"ComplexRepository: Adding new complex: {complex.Name}, Address: {complex.Address}...");
            await _context.Complexes.AddAsync(complex);
            await _context.SaveChangesAsync();
            System.Console.WriteLine($"ComplexRepository: Complex {complex.Name} added successfully with ID {complex.Id}.");
        }

        public async Task UpdateAsync(Complex complex)
        {
            System.Console.WriteLine($"ComplexRepository: Updating complex with ID {complex.Id}, Name: {complex.Name}...");
            _context.Complexes.Update(complex);
            await _context.SaveChangesAsync();
            System.Console.WriteLine($"ComplexRepository: Complex with ID {complex.Id} updated successfully.");
        }

        public async Task DeleteAsync(int id)
        {
            System.Console.WriteLine($"ComplexRepository: Deleting complex with ID {id}...");
            var complex = await _context.Complexes.FindAsync(id);
            if (complex != null)
            {
                _context.Complexes.Remove(complex);
                await _context.SaveChangesAsync();
                System.Console.WriteLine($"ComplexRepository: Complex with ID {id} deleted successfully.");
            }
            else
            {
                System.Console.WriteLine($"ComplexRepository: Complex with ID {id} not found for deletion.");
            }
        }
    }
}