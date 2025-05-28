using Application.DTO;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Deta;
using Infrastructure.Data;

namespace ResidentialReservationSystem.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserRepository _userRepository;

        public ProfileController(ApplicationDbContext context, IUserRepository userRepository)
        {
            _context = context;
            _userRepository = userRepository;
            Console.WriteLine("ProfileController: Profile controller successfully initialized.");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            Console.WriteLine($"ProfileController: Request received to display profile for user: {User.Identity.Name}");
            try
            {
                var username = User.Identity.Name;
                Console.WriteLine($"ProfileController: Fetching user data for {username}...");
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    Console.WriteLine($"ProfileController: Error - User {username} not found.");
                    return NotFound();
                }

                Console.WriteLine($"ProfileController: Fetching reservations for user {username}...");
                var reservations = await _context.Reservations
                    .Where(r => r.UserId == user.Id)
                    .Include(r => r.Unit)
                    .ThenInclude(u => u.Complex)
                    .ToListAsync();

                var profile = new ProfileDTO
                {
                    Username = user.Username,
                    FullName = user.FullName,
                    Role = user.Role,
                    Reservations = reservations
                };

                Console.WriteLine($"ProfileController: Profile data for {username} successfully retrieved.");
                return View(profile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ProfileController: Error while fetching profile - Message: {ex.Message}, StackTrace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while fetching the profile.");
            }
        }
    }
}