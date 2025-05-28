using Application.DTO;
using Application.Services;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ResidentialReservationSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IReservationService _reservationService;
        private readonly IUserRepository _userRepository;

        public AccountController(IAuthService authService, IReservationService reservationService, IUserRepository userRepository)
        {
            _authService = authService;
            _reservationService = reservationService;
            _userRepository = userRepository;
            System.Console.WriteLine("AccountController: Controller initialized with IAuthService, IReservationService, IUserRepository.");
        }

        [HttpGet]
        public IActionResult Login()
        {
            System.Console.WriteLine("AccountController: Displaying Login view...");
            return View(new LoginDTO());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO model)
        {
            System.Console.WriteLine($"AccountController: Login attempt for username: {model.Username}, Provided Password: {model.Password}");
            if (ModelState.IsValid)
            {
                try
                {
                    System.Console.WriteLine("AccountController: Calling AuthService.LoginAsync...");
                    var user = await _authService.LoginAsync(model.Username, model.Password);
                    if (user != null)
                    {
                        System.Console.WriteLine($"AccountController: Login successful for user ID: {user.Id}, Role: {user.Role}");
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                            new Claim(ClaimTypes.Role, user.Role.ToString())
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                        };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        if (user.Role == UserRole.Admin)
                        {
                            System.Console.WriteLine("AccountController: Redirecting Admin to Admin Index...");
                            return RedirectToAction("Index", "Admin");
                        }
                        System.Console.WriteLine("AccountController: Redirecting User to Home Index...");
                        return RedirectToAction("Index", "Home");
                    }
                    System.Console.WriteLine("AccountController: Invalid username or password.");
                    ModelState.AddModelError("", "Invalid username or password.");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"AccountController: Error during login - Message: {ex.Message}, StackTrace: {ex.StackTrace}");
                    ModelState.AddModelError("", "An error occurred while logging in. Please try again.");
                }
            }
            else
            {
                System.Console.WriteLine("AccountController: ModelState is invalid for Login.");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Console.WriteLine($"AccountController: ModelState Error: {error.ErrorMessage}");
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            System.Console.WriteLine("AccountController: Displaying Register view...");
            return View(new RegisterDTO());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDTO model)
        {
            System.Console.WriteLine($"AccountController: Register attempt for username: {model.Username}");
            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _userRepository.GetByUsernameAsync(model.Username);
                    if (existingUser != null)
                    {
                        System.Console.WriteLine("AccountController: Username already exists.");
                        ModelState.AddModelError("", "Username already exists.");
                        return View(model);
                    }

                    var user = new User
                    {
                        Username = model.Username,
                        Password = model.Password,
                        FullName = model.FullName,
                        Role = UserRole.User,
                        Reservations = new List<Reservation>() 
                    };

                    await _authService.RegisterAsync(user);
                    System.Console.WriteLine($"AccountController: User registered successfully - ID: {user.Id}");
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"AccountController: Error during registration - Message: {ex.Message}, StackTrace: {ex.StackTrace}");
                    ModelState.AddModelError("", "An error occurred while registering. Please try again.");
                    return View(model);
                }
            }
            System.Console.WriteLine("AccountController: ModelState is invalid for Register.");
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                System.Console.WriteLine($"AccountController: ModelState Error: {error.ErrorMessage}");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            System.Console.WriteLine("AccountController: Logging out user...");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            System.Console.WriteLine("AccountController: User logged out successfully.");
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            System.Console.WriteLine("AccountController: Displaying AccessDenied view...");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            System.Console.WriteLine("AccountController: Fetching user profile...");
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                System.Console.WriteLine("AccountController: User ID not found in claims for Profile.");
                return RedirectToAction("Login");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                System.Console.WriteLine($"AccountController: User with ID {userId} not found for Profile.");
                return RedirectToAction("Login");
            }

            var reservations = await _reservationService.GetAllReservationsAsync();
            var userReservations = reservations.Where(r => r.UserId == userId).ToList();
            System.Console.WriteLine($"AccountController: Retrieved {userReservations.Count} reservations for user ID {userId}.");

            ViewBag.User = user;
            return View(userReservations);
        }
    }
}