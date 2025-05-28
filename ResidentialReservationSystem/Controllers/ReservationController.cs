using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ResidentialReservationSystem.Controllers
{
    public class ReservationController : Controller
    {
        private readonly IReservationService _reservationService;
        private readonly IUnitService _unitService;

        public ReservationController(IReservationService reservationService, IUnitService unitService)
        {
            _reservationService = reservationService;
            _unitService = unitService;
            System.Console.WriteLine("ReservationController: Controller initialized.");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create(int unitId)
        {
            System.Console.WriteLine($"ReservationController: Create GET called with Unit ID: {unitId}");
            try
            {
                var unit = await _unitService.GetUnitByIdAsync(unitId);
                if (unit == null)
                {
                    System.Console.WriteLine($"ReservationController: Unit with ID {unitId} not found.");
                    return NotFound();
                }

                bool isAvailable = await _unitService.IsUnitAvailableAsync(unitId, DateTime.Today);
                if (!isAvailable)
                {
                    System.Console.WriteLine($"ReservationController: Unit with ID {unitId} is not available on {DateTime.Today:yyyy-MM-dd}.");
                    TempData["Error"] = "This unit is not available for the selected dates.";
                    return RedirectToAction("UnitDetails", "Home", new { id = unitId });
                }

                System.Console.WriteLine($"ReservationController: Unit found - Title: {unit.Title}, IsAvailable: {unit.IsAvailable}");
                ViewBag.Unit = unit;
                var model = new Reservation { UnitId = unitId };
                System.Console.WriteLine("ReservationController: Returning Create view...");
                return View(model);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"ReservationController: Error in Create GET - {ex.Message}");
                return StatusCode(500, "An error occurred while loading the reservation form.");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(Reservation reservation)
        {
            System.Console.WriteLine($"ReservationController: Create POST called for Unit ID: {reservation.UnitId}");

            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                reservation.UserId = userId;
                System.Console.WriteLine($"ReservationController: Creating reservation for User ID: {userId}");

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
                    TempData["Error"] = "The unit is not available for the selected date range.";
                    ViewBag.Unit = unit;
                    return View(reservation);
                }

                if (!await _reservationService.ValidateDatesAsync(reservation.UnitId, reservation.CheckInDate, reservation.CheckOutDate))
                {
                    var unit = await _unitService.GetUnitByIdAsync(reservation.UnitId);
                    string errorMessage = "Selected dates are invalid.";
                    if (unit != null)
                    {
                        if (unit.Rates == null || !unit.Rates.Any())
                        {
                            errorMessage = "No rates defined for this unit.";
                        }
                        else if (unit.HasMandatoryCheckInOut)
                        {
                            var allowedCheckInDays = unit.Rates.Select(r => r.StartDate.DayOfWeek).Distinct();
                            var allowedCheckOutDays = unit.Rates.Select(r => r.EndDate.DayOfWeek).Distinct();
                            errorMessage = $"Selected dates are invalid. Check-in must be on: {string.Join(", ", allowedCheckInDays)}. Check-out must be on: {string.Join(", ", allowedCheckOutDays)}. Available rates: {string.Join(", ", unit.Rates.Select(r => $"{r.StartDate:yyyy-MM-dd} to {r.EndDate:yyyy-MM-dd}"))}.";
                        }
                        else
                        {
                            errorMessage += $" Available rates: {string.Join(", ", unit.Rates.Select(r => $"{r.StartDate:yyyy-MM-dd} to {r.EndDate:yyyy-MM-dd}"))}.";
                        }
                    }
                    TempData["Error"] = errorMessage;
                    ViewBag.Unit = unit;
                    return View(reservation);
                }

                decimal totalPrice = await _reservationService.CalculateTotalPriceAsync(reservation.UnitId, reservation.CheckInDate, reservation.CheckOutDate);
                reservation.TotalPrice = totalPrice;
                System.Console.WriteLine($"ReservationController: Total calculated price is {totalPrice}");

                await _reservationService.CreateReservationAsync(reservation, reservation.UnitId);
                System.Console.WriteLine("ReservationController: Reservation created successfully.");

                TempData["Success"] = $"Reservation created successfully. Total Price: {reservation.TotalPrice:C}";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"ReservationController: Error in Create POST - {ex.Message}");
                TempData["Error"] = ex.Message;
                ViewBag.Unit = await _unitService.GetUnitByIdAsync(reservation.UnitId);
                return View(reservation);
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ConfirmReservation(int id)
        {
            try
            {
                await _reservationService.ConfirmReservationAsync(id);
                TempData["Success"] = "Reservation confirmed successfully.";
                return RedirectToAction("Manage", "Reservation");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Manage", "Reservation");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CancelReservation(int id)
        {
            try
            {
                await _reservationService.CancelReservationAsync(id);
                TempData["Success"] = "Reservation cancelled successfully.";
                return RedirectToAction("Manage", "Reservation");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Manage", "Reservation");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            var reservations = await _reservationService.GetAllReservationsAsync();
            return View(reservations);
        }
    }
}