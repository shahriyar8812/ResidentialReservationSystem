using Domain.Entities;
using Domain.Interfaces;
using System.Threading.Tasks;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
            System.Console.WriteLine("AuthService: Service initialized with IUserRepository.");
        }

        public async Task<User> LoginAsync(string username, string password)
        {
            System.Console.WriteLine($"AuthService: Attempting login for username: {username}");
            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    System.Console.WriteLine("AuthService: Login failed - User not found.");
                    return null;
                }

                System.Console.WriteLine($"AuthService: User found - ID: {user.Id}, Stored Password: {user.Password}, Provided Password: {password}");
                if (user.Password != password)
                {
                    System.Console.WriteLine("AuthService: Login failed - Password mismatch.");
                    return null;
                }

                System.Console.WriteLine($"AuthService: Login successful for user ID: {user.Id}");
                return user;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"AuthService: Error during login - Message: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task RegisterAsync(User user)
        {
            System.Console.WriteLine($"AuthService: Registering new user with username: {user.Username}");
            try
            {
                await _userRepository.AddAsync(user);
                System.Console.WriteLine($"AuthService: User registered successfully - ID: {user.Id}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"AuthService: Error during registration - Message: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
        }
    }
}