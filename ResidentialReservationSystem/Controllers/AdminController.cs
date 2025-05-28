using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Services;
using Domain.Interfaces;
using Infrastructure.Repositories;

namespace ResidentialReservationSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUnitService _unitService;
        private readonly IReservationService _reservationService;
        private readonly IUserRepository _userRepository;
        private readonly IFeatureRepository _featureRepository;
        private readonly ICheckInRuleRepository _checkInRuleRepository;
        private readonly IUnitRepository _unitRepository;

        public AdminController(
            IUnitService unitService,
            IReservationService reservationService,
            IUserRepository userRepository,
            IFeatureRepository featureRepository,
            ICheckInRuleRepository checkInRuleRepository,
            IUnitRepository unitRepository)
        {
            _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
            _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _featureRepository = featureRepository ?? throw new ArgumentNullException(nameof(featureRepository));
            _checkInRuleRepository = checkInRuleRepository ?? throw new ArgumentNullException(nameof(checkInRuleRepository));
            _unitRepository = unitRepository ?? throw new ArgumentNullException(nameof(unitRepository));
            Console.WriteLine("AdminController: Initialized successfully with dependencies.");
        }

        [HttpGet]
        public IActionResult Index()
        {
            Console.WriteLine("AdminController: Index method called. Displaying Index view...");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            try
            {
                Console.WriteLine("AdminController: Users method called. Fetching all users...");
                var users = await _userRepository.GetAllAsync();
                Console.WriteLine($"AdminController: Retrieved {users.Count} users.");
                return View(users);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: Error in Users method: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = "An error occurred while fetching users.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> UserDetails(int id)
        {
            Console.WriteLine($"AdminController: UserDetails method called for ID {id}...");
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    Console.WriteLine($"AdminController: User with ID {id} not found.");
                    return NotFound();
                }
                var reservations = await _reservationService.GetAllReservationsAsync();
                var userReservations = reservations.Where(r => r.UserId == id).ToList();
                Console.WriteLine($"AdminController: Retrieved {userReservations.Count} reservations for user ID {id}.");
                ViewBag.User = user;
                return View(userReservations);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: Error in UserDetails method for ID {id}: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = "An error occurred while fetching user details.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            Console.WriteLine($"AdminController: DeleteUser method called for ID {id}...");
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    Console.WriteLine($"AdminController: User with ID {id} not found.");
                    return NotFound();
                }

                if (user.Role == UserRole.Admin)
                {
                    Console.WriteLine($"AdminController: Cannot delete admin user with ID {id}.");
                    TempData["Error"] = "Cannot delete an admin user.";
                    return RedirectToAction("Users");
                }

                await _userRepository.DeleteAsync(id);
                Console.WriteLine($"AdminController: User with ID {id} deleted successfully.");
                TempData["Success"] = "User deleted successfully.";
                return RedirectToAction("Users");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: Error in DeleteUser method for ID {id}: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"An error occurred while deleting the user: {ex.Message}";
                return RedirectToAction("Users");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Units()
        {
            try
            {
                Console.WriteLine("AdminController: Units method called. Fetching all units...");
                var units = await _unitService.GetAllUnitsAsync();
                Console.WriteLine($"AdminController: Retrieved {units.Count} units.");
                return View(units);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: Error in Units method: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = "An error occurred while fetching units.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateUnit()
        {
            try
            {
                Console.WriteLine("AdminController: CreateUnit GET method called. Preparing view...");
                ViewBag.Complexes = await _unitService.GetAllComplexesAsync();
                Console.WriteLine($"AdminController: Retrieved {ViewBag.Complexes.Count} complexes for CreateUnit.");

                var features = await _featureRepository.GetAllAsync();
                Console.WriteLine($"AdminController: Retrieved {features.Count} features for CreateUnit.");

                var unit = new Unit
                {
                    UnitFeatures = features.Select(f => new UnitFeature { FeatureId = f.Id }).ToList()
                };
                ViewBag.Features = features;
                Console.WriteLine("AdminController: Displaying CreateUnit view with prepared data...");
                return View(unit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: Error in CreateUnit GET method: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = "An error occurred while preparing the Create Unit page.";
                return RedirectToAction("Units");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUnit(
      Unit unit,
      List<IFormFile> images,
      List<int> selectedFeatureIds,
      List<string> ratesStartDates,
      List<string> ratesEndDates,
      List<decimal> ratesPrices)
        {
            Console.WriteLine("AdminController: CreateUnit POST called...");

            try
            {
                unit.UnitImages = new List<UnitImage>();
                if (images != null && images.Any())
                {
                    foreach (var image in images)
                    {
                        if (image.Length > 0)
                        {
                            var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                            using var stream = new FileStream(filePath, FileMode.Create);
                            await image.CopyToAsync(stream);
                            unit.UnitImages.Add(new UnitImage { ImageUrl = "/images/" + fileName });
                        }
                    }
                }

                unit.UnitFeatures = selectedFeatureIds?
                    .Select(id => new UnitFeature { FeatureId = id })
                    .ToList() ?? new List<UnitFeature>();

                var newRates = new List<Rate>();
                for (int i = 0; i < ratesStartDates.Count; i++)
                {
                    if (Enum.TryParse<DayOfWeek>(ratesStartDates[i], out var startDay) &&
                        Enum.TryParse<DayOfWeek>(ratesEndDates[i], out var endDay) &&
                        ratesPrices[i] > 0)
                    {
                        var startDate = _unitService.GetFirstOccurrenceInWeek(startDay, DateTime.Today);
                        var endDate = _unitService.GetLastOccurrenceInWeek(endDay, startDate);

                        newRates.Add(new Rate
                        {
                            StartDate = startDate,
                            EndDate = endDate,
                            PricePerNight = ratesPrices[i]
                        });
                    }
                }

                unit.IsAvailable = true;

                await _unitService.AddUnitAsync(unit);
                await _unitService.UpdateRatesAsync(unit, newRates, new List<int>());

                TempData["Success"] = "واحد با موفقیت ایجاد شد.";
                return RedirectToAction("Units");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطا در ثبت واحد: {ex.Message}";
                ViewBag.Complexes = await _unitService.GetAllComplexesAsync();
                ViewBag.Features = await _featureRepository.GetAllAsync();
                return View(unit);
            }
        }


        [HttpGet]
        public async Task<IActionResult> EditUnit(int id)
        {
            Console.WriteLine($"AdminController: EditUnit GET method called for ID {id}...");
            try
            {
                var unit = await _unitService.GetUnitByIdAsync(id);
                if (unit == null)
                {
                    Console.WriteLine($"AdminController: Unit with ID {id} not found.");
                    return NotFound();
                }

                var features = await _featureRepository.GetAllAsync();
                Console.WriteLine($"AdminController: Retrieved {features.Count} features for EditUnit.");
                ViewBag.Features = features;
                ViewBag.Complexes = await _unitService.GetAllComplexesAsync();
                ViewBag.UnitImages = unit.UnitImages?.ToList() ?? new List<UnitImage>();
                ViewBag.SelectedFeatureIds = unit.UnitFeatures?.Select(uf => uf.FeatureId).ToList() ?? new List<int>();

                var rates = await _unitRepository.GetRatesForUnit(id) ?? new List<Rate>();
                ViewBag.Rates = rates;

                ViewBag.ExistingRateIds = rates.Select(r => r.Id).ToList();

                Console.WriteLine($"AdminController: Found unit with ID {id}, rates count: {rates.Count}");
                return View(unit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: Error in EditUnit GET method: {ex.Message}");
                TempData["Error"] = "خطا در بارگذاری اطلاعات واحد.";
                return RedirectToAction("Units");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditUnit(
    Unit unit,
    List<IFormFile> images,
    List<int> selectedFeatureIds,
    Dictionary<string, bool> deleteImage,
    Dictionary<string, bool> deleteRate,
    List<string> ratesStartDates,
    List<string> ratesEndDates,
    List<decimal> ratesPrices)
        {
            Console.WriteLine($"AdminController: EditUnit POST method called for Unit ID: {unit.Id}");

            Unit existingUnit = null;

            try
            {
                existingUnit = await _unitService.GetUnitByIdAsync(unit.Id);
                if (existingUnit == null)
                {
                    TempData["Error"] = "Unit not found.";
                    return NotFound();
                }

                existingUnit.Title = unit.Title;
                existingUnit.ComplexId = unit.ComplexId;
                existingUnit.Description = unit.Description;
                existingUnit.Capacity = unit.Capacity;
                existingUnit.BedroomCount = unit.BedroomCount;
                existingUnit.HasMandatoryCheckInOut = unit.HasMandatoryCheckInOut;

                if (deleteImage != null)
                {
                    var idsToDelete = deleteImage
                        .Where(x => x.Value && x.Key.StartsWith("deleteImage_"))
                        .Select(x => int.Parse(x.Key.Replace("deleteImage_", "")))
                        .ToList();

                    existingUnit.UnitImages = existingUnit.UnitImages
                        .Where(img =>
                        {
                            if (!idsToDelete.Contains(img.Id)) return true;

                            if (!string.IsNullOrWhiteSpace(img.ImageUrl))
                            {
                                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", img.ImageUrl.TrimStart('/'));
                                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                            }
                            return false;
                        }).ToList();
                }

                // Upload new images
                if (images != null && images.Any())
                {
                    existingUnit.UnitImages ??= new List<UnitImage>();
                    foreach (var image in images)
                    {
                        if (image.Length > 0)
                        {
                            var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                            using var stream = new FileStream(filePath, FileMode.Create);
                            await image.CopyToAsync(stream);
                            existingUnit.UnitImages.Add(new UnitImage
                            {
                                UnitId = unit.Id,
                                ImageUrl = "/images/" + fileName
                            });
                        }
                    }
                }

                // Rates
                var existingRateIds = Request.Form["existingRateIds"]
                    .Select(id => int.TryParse(id, out var parsed) ? parsed : -1)
                    .Where(id => id > 0)
                    .ToList();

                var rateIdsToDelete = deleteRate?
                    .Where(x => x.Value && x.Key.StartsWith("deleteRate_"))
                    .Select(x => int.TryParse(x.Key.Replace("deleteRate_", ""), out var id) ? id : -1)
                    .Where(id => id > 0)
                    .ToList() ?? new List<int>();

                var newRates = new List<Rate>();
                for (int i = 0; i < ratesStartDates.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(ratesStartDates[i]) ||
                        string.IsNullOrWhiteSpace(ratesEndDates[i]) ||
                        ratesPrices[i] <= 0) continue;

                    if (Enum.TryParse<DayOfWeek>(ratesStartDates[i], out var startDay) &&
                        Enum.TryParse<DayOfWeek>(ratesEndDates[i], out var endDay))
                    {
                        var baseStartDate = _unitService.GetFirstOccurrenceInWeek(startDay, DateTime.Today);
                        var startDate = baseStartDate;
                        var endDate = _unitService.GetLastOccurrenceInWeek(endDay, startDate);

                        var rate = new Rate
                        {
                            UnitId = unit.Id,
                            StartDate = startDate,
                            EndDate = endDate,
                            PricePerNight = ratesPrices[i],
                            Id = existingRateIds.ElementAtOrDefault(i)
                        };

                        if (!rateIdsToDelete.Contains(rate.Id))
                        {
                            newRates.Add(rate);
                        }
                    }
                }

                await _unitService.UpdateRatesAsync(existingUnit, newRates, rateIdsToDelete);

                // Features
                existingUnit.UnitFeatures = selectedFeatureIds?
                    .Select(fid => new UnitFeature { UnitId = unit.Id, FeatureId = fid })
                    .ToList() ?? new List<UnitFeature>();

                // Availability
                var reservations = await _reservationService.GetAllReservationsAsync();
                existingUnit.IsAvailable = !reservations.Any(r =>
                    r.UnitId == unit.Id &&
                    r.CheckOutDate >= DateTime.Now &&
                    r.Status == ReservationStatus.Confirmed);

                await _unitService.UpdateUnitAsync(existingUnit);

                TempData["Success"] = "واحد با موفقیت ویرایش شد.";
                return RedirectToAction("Units");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: EditUnit error: {ex.Message}");
                TempData["Error"] = $"خطا در ویرایش واحد: {ex.Message}";
                ViewBag.Complexes = await _unitService.GetAllComplexesAsync();
                ViewBag.Features = await _featureRepository.GetAllAsync();
                ViewBag.UnitImages = existingUnit?.UnitImages ?? new List<UnitImage>();
                ViewBag.SelectedFeatureIds = selectedFeatureIds;
                ViewBag.Rates = await _unitRepository.GetRatesForUnit(unit.Id) ?? new List<Rate>();
                unit.UnitImages = existingUnit?.UnitImages ?? new List<UnitImage>();
                unit.UnitFeatures = existingUnit?.UnitFeatures ?? new List<UnitFeature>();
                return View(unit);
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteUnit(int id)
        {
            Console.WriteLine($"AdminController: DeleteUnit method called for ID {id}...");
            try
            {
                await _unitService.DeleteUnitAsync(id);
                Console.WriteLine($"AdminController: Unit with ID {id} deleted successfully.");
                TempData["Success"] = "Unit deleted successfully.";
                return RedirectToAction("Units");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: Error in DeleteUnit method for ID {id}: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"An error occurred while deleting the unit: {ex.Message}";
                return RedirectToAction("Units");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Reservations()
        {
            try
            {
                Console.WriteLine("AdminController: Reservations method called. Fetching all reservations...");
                var reservations = await _reservationService.GetAllReservationsAsync();
                Console.WriteLine($"AdminController: Retrieved {reservations.Count} reservations.");
                return View(reservations);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: Error in Reservations method: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = "An error occurred while fetching reservations.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CalculateTotalPriceForExistingReservation(int id)
        {
            Console.WriteLine($"AdminController: CalculateTotalPriceForExistingReservation method called for ID {id}...");
            try
            {
                var reservation = await _reservationService.GetReservationByIdAsync(id);
                if (reservation == null)
                {
                    Console.WriteLine($"AdminController: Reservation with ID {id} not found.");
                    throw new Exception("Reservation not found.");
                }
                reservation.TotalPrice = await _reservationService.CalculateTotalPriceAsync(reservation.UnitId, reservation.CheckInDate, reservation.CheckOutDate);
                await _reservationService.CreateReservationAsync(reservation, reservation.UnitId);
                Console.WriteLine($"AdminController: TotalPrice calculated for reservation ID {id}.");
                TempData["Success"] = "TotalPrice calculated successfully.";
                return RedirectToAction("Reservations");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: Error in CalculateTotalPriceForExistingReservation for ID {id}: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("Reservations");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmReservation(int id)
        {
            Console.WriteLine($"AdminController: ConfirmReservation method called for ID {id}...");
            try
            {
                await _reservationService.ConfirmReservationAsync(id);
                Console.WriteLine($"AdminController: Reservation with ID {id} confirmed successfully.");
                TempData["Success"] = "Reservation confirmed successfully.";
                return RedirectToAction("Reservations");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: Error in ConfirmReservation for ID {id}: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"An error occurred while confirming the reservation: {ex.Message}";
                return RedirectToAction("Reservations");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelReservation(int id)
        {
            Console.WriteLine($"AdminController: CancelReservation method called for ID {id}...");
            try
            {
                await _reservationService.CancelReservationAsync(id);
                Console.WriteLine($"AdminController: Reservation with ID {id} cancelled successfully.");
                TempData["Success"] = "Reservation cancelled successfully.";
                return RedirectToAction("Reservations");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: Error in CancelReservation for ID {id}: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"An error occurred while cancelling the reservation: {ex.Message}";
                return RedirectToAction("Reservations");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Complexes()
        {
            try
            {
                Console.WriteLine("AdminController: Complexes method called. Fetching all complexes...");
                var complexes = await _unitService.GetAllComplexesAsync();
                Console.WriteLine($"AdminController: Retrieved {complexes.Count} complexes.");
                return View(complexes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: Error in Complexes method: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = "An error occurred while fetching complexes.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult CreateComplex()
        {
            Console.WriteLine("AdminController: CreateComplex GET method called. Displaying view...");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateComplex(ComplexCreateDto complexDto)
        {
            Console.WriteLine($"AdminController: CreateComplex POST method called - Name: {complexDto.Name}, Address: {complexDto.Address}");
            if (ModelState.IsValid)
            {
                var complex = new Complex
                {
                    Name = complexDto.Name,
                    Address = complexDto.Address
                };
                Console.WriteLine("AdminController: ModelState is valid, proceeding to add complex...");
                await _unitService.AddComplexAsync(complex);
                Console.WriteLine("AdminController: Complex added successfully, redirecting to Complexes...");
                return RedirectToAction("Complexes");
            }
            Console.WriteLine("AdminController: ModelState is invalid, returning to CreateComplex view...");
            foreach (var state in ModelState)
            {
                if (state.Value.Errors.Any())
                {
                    Console.WriteLine($"AdminController: ModelState Error for {state.Key}: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }
            return View(complexDto);
        }

        [HttpGet]
        public async Task<IActionResult> EditComplex(int id)
        {
            Console.WriteLine($"AdminController: EditComplex GET method called for ID {id}...");
            try
            {
                var complex = await _unitService.GetComplexByIdAsync(id);
                if (complex == null)
                {
                    Console.WriteLine($"AdminController: Complex with ID {id} not found.");
                    return NotFound();
                }
                var complexDto = new ComplexCreateDto
                {
                    Id = complex.Id,
                    Name = complex.Name,
                    Address = complex.Address
                };
                Console.WriteLine($"AdminController: Found complex with ID {id}, displaying EditComplex view...");
                return View(complexDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: Error in EditComplex GET method for ID {id}: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = "An error occurred while fetching the complex for editing.";
                return RedirectToAction("Complexes");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditComplex(ComplexCreateDto complexDto)
        {
            Console.WriteLine($"AdminController: EditComplex POST method called - ID: {complexDto.Id}, Name: {complexDto.Name}, Address: {complexDto.Address}");
            if (ModelState.IsValid)
            {
                var complex = new Complex
                {
                    Id = complexDto.Id,
                    Name = complexDto.Name,
                    Address = complexDto.Address
                };
                Console.WriteLine("AdminController: ModelState is valid, proceeding to update complex...");
                await _unitService.UpdateComplexAsync(complex);
                Console.WriteLine("AdminController: Complex updated successfully, redirecting to Complexes...");
                return RedirectToAction("Complexes");
            }
            Console.WriteLine("AdminController: ModelState is invalid, returning to EditComplex view...");
            foreach (var state in ModelState)
            {
                if (state.Value.Errors.Any())
                {
                    Console.WriteLine($"AdminController: ModelState Error for {state.Key}: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }
            return View(complexDto);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteComplex(int id)
        {
            Console.WriteLine($"AdminController: DeleteComplex method called for ID {id}...");
            try
            {
                await _unitService.DeleteComplexAsync(id);
                Console.WriteLine("AdminController: Complex deleted successfully.");
                TempData["Success"] = "Complex deleted successfully.";
                return RedirectToAction("Complexes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: Error in DeleteComplex method for ID {id}: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("Complexes");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManageCheckInRules(int unitId)
        {
            Console.WriteLine($"AdminController: ManageCheckInRules GET method called for Unit ID {unitId}...");
            try
            {
                var unit = await _unitService.GetUnitByIdAsync(unitId);
                if (unit == null)
                {
                    Console.WriteLine($"AdminController: Unit with ID {unitId} not found.");
                    return NotFound();
                }

                var rule = await _checkInRuleRepository.GetByUnitIdAsync(unitId);
                var model = new CheckInRule
                {
                    Id = rule?.Id ?? 0,
                    UnitId = unitId,
                    Unit = unit,
                    AllowedCheckInDay = rule?.AllowedCheckInDay ?? DayOfWeek.Saturday,
                    AllowedCheckOutDay = rule?.AllowedCheckOutDay ?? DayOfWeek.Friday
                };
                Console.WriteLine($"AdminController: Prepared model for view - Rule ID: {model.Id}, UnitId: {model.UnitId}, CheckIn: {model.AllowedCheckInDay}, CheckOut: {model.AllowedCheckOutDay}");
                Console.WriteLine($"AdminController: Displaying ManageCheckInRules view for Unit ID {unitId}.");
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: Error in ManageCheckInRules GET method: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = "An error occurred while loading the check-in rules page.";
                return RedirectToAction("Units");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ManageCheckInRules(CheckInRule rule)
        {
            Console.WriteLine($"AdminController: ManageCheckInRules POST method called for Unit ID {rule.UnitId}, CheckInDay: {rule.AllowedCheckInDay}, CheckOutDay: {rule.AllowedCheckOutDay}, Rule ID: {rule.Id}");
            try
            {
                ModelState.Clear();

                var existingRule = await _checkInRuleRepository.GetByUnitIdAsync(rule.UnitId);
                if (existingRule == null)
                {
                    var newRule = new CheckInRule
                    {
                        UnitId = rule.UnitId,
                        AllowedCheckInDay = rule.AllowedCheckInDay,
                        AllowedCheckOutDay = rule.AllowedCheckOutDay
                    };
                    await _checkInRuleRepository.AddAsync(newRule);
                    Console.WriteLine($"AdminController: Added new check-in rule for Unit ID {rule.UnitId}");
                }
                else
                {
                    existingRule.AllowedCheckInDay = rule.AllowedCheckInDay;
                    existingRule.AllowedCheckOutDay = rule.AllowedCheckOutDay;
                    await _checkInRuleRepository.UpdateAsync(existingRule);
                    Console.WriteLine($"AdminController: Updated check-in rule with ID {existingRule.Id} for Unit ID {rule.UnitId}");
                }

                TempData["Success"] = "Check-in rules updated successfully.";
                Console.WriteLine("AdminController: Redirecting to Units after successful update.");
                return RedirectToAction("Units");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminController: Error in ManageCheckInRules POST method: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"An error occurred while updating check-in rules: {ex.Message}";
                rule.Unit = await _unitService.GetUnitByIdAsync(rule.UnitId);
                return View(rule);
            }
        }
    }
}