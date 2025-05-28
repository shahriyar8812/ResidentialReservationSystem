using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
            System.Console.WriteLine("UserRepository: Repository initialized.");
        }

        public async Task<List<User>> GetAllAsync()
        {
            System.Console.WriteLine("UserRepository: Fetching all users...");
            try
            {
                var users = await _context.Users
                    .AsNoTracking()
                    .ToListAsync();
                System.Console.WriteLine($"UserRepository: Retrieved {users.Count} users.");
                return users;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"UserRepository: Error fetching all users - Message: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<User> GetByIdAsync(int id)
        {
            System.Console.WriteLine($"UserRepository: Fetching user with ID {id}...");
            try
            {
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                {
                    System.Console.WriteLine($"UserRepository: User with ID {id} not found.");
                }
                else
                {
                    System.Console.WriteLine($"UserRepository: User found - ID: {user.Id}, Username: {user.Username}");
                }
                return user;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"UserRepository: Error fetching user with ID {id} - Message: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            System.Console.WriteLine($"UserRepository: Fetching user with username {username}...");
            try
            {
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    System.Console.WriteLine($"UserRepository: User with username {username} not found.");
                }
                else
                {
                    System.Console.WriteLine($"UserRepository: User found - ID: {user.Id}, Username: {user.Username}, Role: {user.Role}, Password: {user.Password}");
                }
                return user;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"UserRepository: Error fetching user with username {username} - Message: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task AddAsync(User user)
        {
            System.Console.WriteLine($"UserRepository: Adding new user with username: {user.Username}...");
            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
                System.Console.WriteLine($"UserRepository: User added successfully with ID: {user.Id}.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"UserRepository: Error adding user - Message: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task UpdateAsync(User user)
        {
            System.Console.WriteLine($"UserRepository: Updating user with ID {user.Id}...");
            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                System.Console.WriteLine($"UserRepository: User with ID {user.Id} updated successfully.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"UserRepository: Error updating user with ID {user.Id} - Message: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            System.Console.WriteLine($"UserRepository: Deleting user with ID {id}...");
            try
            {
                var user = await GetByIdAsync(id);
                if (user != null)
                {
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                    System.Console.WriteLine($"UserRepository: User with ID {id} deleted successfully.");
                }
                else
                {
                    System.Console.WriteLine($"UserRepository: User with ID {id} not found for deletion.");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"UserRepository: Error deleting user with ID {id} - Message: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
        }
    }
}