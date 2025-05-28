using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities;
using System.Security.Claims;
using Domain.Interfaces;
using ResidentialReservationSystem.Models;
using Microsoft.AspNetCore.Authorization;

namespace ResidentialReservationSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUnitService _unitService;
        private readonly IReservationService _reservationService;
        private readonly IUserRepository _userRepository;

        public HomeController(
            IUnitService unitService,
            IReservationService reservationService,
            IUserRepository userRepository)
        {
            _unitService = unitService;
            _reservationService = reservationService;
            _userRepository = userRepository;
            System.Console.WriteLine("HomeController: Controller initialized.");
        }

        public async Task<IActionResult> Index()
        {
            System.Console.WriteLine("HomeController: Fetching all units for Index...");
            var units = await _unitService.GetAllUnitsAsync();
            System.Console.WriteLine($"HomeController: Retrieved {units.Count} units.");
            if (units.Count == 0)
            {
                System.Console.WriteLine("HomeController: No units found in the database.");
            }
            else
            {
                foreach (var unit in units)
                {
                    System.Console.WriteLine($"HomeController: Unit - ID: {unit.Id}, Title: {unit.Title}, Complex: {unit.Complex?.Name}, IsAvailable: {unit.IsAvailable}");
                }
            }

            System.Console.WriteLine("HomeController: Fetching all complexes for Index...");
            var complexes = await _unitService.GetAllComplexesAsync();
            System.Console.WriteLine($"HomeController: Retrieved {complexes.Count} complexes.");
            if (complexes.Count == 0)
            {
                System.Console.WriteLine("HomeController: No complexes found in the database.");
            }
            else
            {
                foreach (var complex in complexes)
                {
                    System.Console.WriteLine($"HomeController: Complex - ID: {complex.Id}, Name: {complex.Name}");
                }
            }

            ViewBag.Complexes = complexes;
            return View(units);
        }

        public async Task<IActionResult> UnitsByComplex(int complexId)
        {
            System.Console.WriteLine($"HomeController: Fetching units for Complex ID {complexId}...");
            var units = await _unitService.GetAllUnitsAsync();
            var filteredUnits = units.Where(u => u.ComplexId == complexId).ToList();
            System.Console.WriteLine($"HomeController: Retrieved {filteredUnits.Count} units for Complex ID {complexId}");

            var complex = await _unitService.GetComplexByIdAsync(complexId);
            ViewBag.ComplexName = complex?.Name ?? "Unknown Complex";
            return View(filteredUnits);
        }

        public async Task<IActionResult> UnitDetails(int id)
        {
            System.Console.WriteLine($"HomeController: Fetching details for Unit ID {id}...");
            var unit = await _unitService.GetUnitByIdAsync(id);
            if (unit == null)
            {
                System.Console.WriteLine($"HomeController: Unit with ID {id} not found.");
                return NotFound();
            }
            System.Console.WriteLine($"HomeController: Unit found - Title: {unit.Title}, Images: {(unit.UnitImages?.Count ?? 0)}, Features: {(unit.UnitFeatures?.Count ?? 0)}, IsAvailable: {unit.IsAvailable}");
            return View(unit);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Reserve(int unitId)
        {
            System.Console.WriteLine($"HomeController: Displaying Reserve view for Unit ID {unitId}...");
            var unit = await _unitService.GetUnitByIdAsync(unitId);
            if (unit == null)
            {
                System.Console.WriteLine($"HomeController: Unit with ID {unitId} not found.");
                return NotFound();
            }

            bool isAvailable = await _unitService.IsUnitAvailableAsync(unitId, DateTime.Today);
            if (!isAvailable)
            {
                System.Console.WriteLine($"HomeController: Unit with ID {unitId} is not available on {DateTime.Today:yyyy-MM-dd}.");
                TempData["Error"] = "This unit is not available for the selected dates.";
                return RedirectToAction("UnitDetails", new { id = unitId });
            }

            var existingReservations = await _reservationService.GetAllReservationsAsync();
            var unitReservations = existingReservations.Where(r => r.UnitId == unitId).ToList();
            ViewBag.ExistingReservations = unitReservations;

            ViewBag.Unit = unit;

            var model = new Reservation
            {
                UnitId = unitId,
                CheckInDate = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(1)
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Reserve(Reservation reservation)
        {
            System.Console.WriteLine($"HomeController: Received Reserve request for Unit ID {reservation.UnitId}...");

            if (reservation.UnitId <= 0)
            {
                System.Console.WriteLine($"HomeController: Invalid Unit ID {reservation.UnitId}.");
                return NotFound();
            }

            var existingReservations = await _reservationService.GetAllReservationsAsync();
            var unitReservations = existingReservations.Where(r => r.UnitId == reservation.UnitId).ToList();
            ViewBag.ExistingReservations = unitReservations;

            bool isAvailableForRange = true;
            for (var date = reservation.CheckInDate; date < reservation.CheckOutDate; date = date.AddDays(1))
            {
                if (!await _unitService.IsUnitAvailableAsync(reservation.UnitId, date))
                {
                    isAvailableForRange = false;
                    break;
                }
            }

            if (!isAvailableForRange)
            {
                var unit = await _unitService.GetUnitByIdAsync(reservation.UnitId);
                ModelState.AddModelError("", "The unit is not available for the selected date range.");
                ViewBag.Unit = unit;
                return View(reservation);
            }

            if (!await _reservationService.ValidateDatesAsync(reservation.UnitId, reservation.CheckInDate, reservation.CheckOutDate))
            {
                var unit = await _unitService.GetUnitByIdAsync(reservation.UnitId);
                string errorMessage;
                if (unit == null || unit.Rates == null || !unit.Rates.Any())
                {
                    errorMessage = "No rates defined for this unit.";
                }
                else if (unit.HasMandatoryCheckInOut)
                {
                    errorMessage = "Selected dates are invalid. Check-in must be on: Sunday, Friday. Check-out must be on: Thursday, Saturday.";
                }
                else
                {
                    errorMessage = "Selected dates are invalid.";
                }
                ModelState.AddModelError("", errorMessage);
                ViewBag.Unit = unit;
                return View(reservation);
            }

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    System.Console.WriteLine("HomeController: User ID not found in claims.");
                    return Unauthorized();
                }

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    System.Console.WriteLine($"HomeController: User with ID {userId} not found.");
                    return Unauthorized();
                }

                reservation.UserId = user.Id;
                await _reservationService.CreateReservationAsync(reservation, reservation.UnitId);
                System.Console.WriteLine($"HomeController: Reservation created successfully for Unit ID {reservation.UnitId}.");
                TempData["Success"] = "Reservation created successfully. Awaiting confirmation.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"HomeController: Error creating reservation: {ex.Message}");
                ModelState.AddModelError("", ex.Message);
                return View(reservation);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}