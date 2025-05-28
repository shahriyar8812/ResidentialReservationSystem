using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities;
using Domain.Interfaces;

namespace ResidentialReservationSystem.Controllers
{
    public class UnitsController : Controller
    {
        private readonly IUnitService _unitService;
        private readonly IComplexRepository _complexRepository;
        private readonly IFeatureRepository _featureRepository;

        public UnitsController(
            IUnitService unitService,
            IComplexRepository complexRepository,
            IFeatureRepository featureRepository)
        {
            _unitService = unitService;
            _complexRepository = complexRepository;
            _featureRepository = featureRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var units = await _unitService.GetAllUnitsAsync();
                return View(units);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while fetching units.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateUnit()
        {
            try
            {
                ViewBag.Complexes = await _complexRepository.GetAllAsync();
                ViewBag.Features = await _featureRepository.GetAllAsync();
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading form: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUnit(Unit unit, List<IFormFile> images, List<int> selectedFeatureIds, List<string> ratesStartDates, List<string> ratesEndDates, List<decimal> ratesPrices)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Complexes = await _complexRepository.GetAllAsync();
                    ViewBag.Features = await _featureRepository.GetAllAsync();
                    return View(unit);
                }

                unit.UnitImages = new List<UnitImage>();
                if (images != null && images.Any())
                {
                    foreach (var image in images)
                    {
                        if (image.Length > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }
                            unit.UnitImages.Add(new UnitImage { ImageUrl = "/images/" + fileName });
                        }
                    }
                }

                unit.UnitFeatures = selectedFeatureIds?.Select(id => new UnitFeature { FeatureId = id }).ToList() ?? new List<UnitFeature>();

                unit.Rates = new List<Rate>();
                if (ratesStartDates != null && ratesEndDates != null && ratesPrices != null)
                {
                    for (int i = 0; i < ratesStartDates.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(ratesStartDates[i]) && !string.IsNullOrEmpty(ratesEndDates[i]) && ratesPrices[i] > 0)
                        {
                            var startDay = Enum.Parse<DayOfWeek>(ratesStartDates[i]);
                            var endDay = Enum.Parse<DayOfWeek>(ratesEndDates[i]);
                            var startDate = new DateTime(2025, 1, 1);
                            var endDate = new DateTime(2025, 12, 31);

                            int daysUntilStartDay = ((int)startDay - (int)startDate.DayOfWeek + 7) % 7;
                            startDate = startDate.AddDays(daysUntilStartDay);

                            int daysUntilEndDay = ((int)endDay - (int)endDate.DayOfWeek + 7) % 7;
                            endDate = endDate.AddDays(-daysUntilEndDay);

                            unit.Rates.Add(new Rate
                            {
                                StartDate = startDate,
                                EndDate = endDate,
                                PricePerNight = ratesPrices[i]
                            });
                        }
                    }
                }

                await _unitService.AddUnitAsync(unit);
                TempData["Success"] = "Unit created successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating unit: {ex.Message}";
                ViewBag.Complexes = await _complexRepository.GetAllAsync();
                ViewBag.Features = await _featureRepository.GetAllAsync();
                return View(unit);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditUnit(int id)
        {
            try
            {
                var unit = await _unitService.GetUnitByIdAsync(id);
                if (unit == null)
                {
                    return NotFound();
                }

                ViewBag.Complexes = await _complexRepository.GetAllAsync();
                ViewBag.Features = await _featureRepository.GetAllAsync();
                ViewBag.UnitImages = unit.UnitImages?.ToList() ?? new List<UnitImage>();
                ViewBag.SelectedFeatureIds = unit.UnitFeatures?.Select(uf => uf.FeatureId).ToList() ?? new List<int>();
                return View(unit);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading unit: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditUnit(Unit unit, List<IFormFile> images, Dictionary<string, bool> deleteImage, List<int> selectedFeatureIds, List<string> ratesStartDates, List<string> ratesEndDates, List<decimal> ratesPrices)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Complexes = await _complexRepository.GetAllAsync();
                    ViewBag.Features = await _featureRepository.GetAllAsync();
                    ViewBag.UnitImages = unit.UnitImages?.ToList() ?? new List<UnitImage>();
                    ViewBag.SelectedFeatureIds = selectedFeatureIds ?? new List<int>();
                    return View(unit);
                }

                var existingUnit = await _unitService.GetUnitByIdAsync(unit.Id);
                if (existingUnit == null)
                {
                    return NotFound();
                }

                existingUnit.Title = unit.Title;
                existingUnit.ComplexId = unit.ComplexId;
                existingUnit.Description = unit.Description;
                existingUnit.Capacity = unit.Capacity;
                existingUnit.BedroomCount = unit.BedroomCount;
                existingUnit.HasMandatoryCheckInOut = unit.HasMandatoryCheckInOut;

                if (deleteImage != null && deleteImage.Any())
                {
                    var imagesToDelete = existingUnit.UnitImages.Where(img => deleteImage.ContainsKey($"deleteImage_{img.Id}") && deleteImage[$"deleteImage_{img.Id}"]).ToList();
                    foreach (var img in imagesToDelete)
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", img.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                    existingUnit.UnitImages = existingUnit.UnitImages.Except(imagesToDelete).ToList();
                }

                if (images != null && images.Any())
                {
                    if (existingUnit.UnitImages == null)
                    {
                        existingUnit.UnitImages = new List<UnitImage>();
                    }
                    foreach (var image in images)
                    {
                        if (image.Length > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }
                            existingUnit.UnitImages.Add(new UnitImage { ImageUrl = "/images/" + fileName });
                        }
                    }
                }

                existingUnit.UnitFeatures = selectedFeatureIds?.Select(id => new UnitFeature { FeatureId = id, UnitId = unit.Id }).ToList() ?? new List<UnitFeature>();

                existingUnit.Rates = new List<Rate>();
                if (ratesStartDates != null && ratesEndDates != null && ratesPrices != null)
                {
                    for (int i = 0; i < ratesStartDates.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(ratesStartDates[i]) && !string.IsNullOrEmpty(ratesEndDates[i]) && ratesPrices[i] > 0)
                        {
                            var startDay = Enum.Parse<DayOfWeek>(ratesStartDates[i]);
                            var endDay = Enum.Parse<DayOfWeek>(ratesEndDates[i]);
                            var startDate = new DateTime(2025, 1, 1);
                            var endDate = new DateTime(2025, 12, 31);

                            int daysUntilStartDay = ((int)startDay - (int)startDate.DayOfWeek + 7) % 7;
                            startDate = startDate.AddDays(daysUntilStartDay);

                            int daysUntilEndDay = ((int)endDay - (int)endDate.DayOfWeek + 7) % 7;
                            endDate = endDate.AddDays(-daysUntilEndDay);

                            existingUnit.Rates.Add(new Rate
                            {
                                StartDate = startDate,
                                EndDate = endDate,
                                PricePerNight = ratesPrices[i]
                            });
                        }
                    }
                }

                await _unitService.UpdateUnitAsync(existingUnit);
                TempData["Success"] = "Unit updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating unit: {ex.Message}";
                ViewBag.Complexes = await _complexRepository.GetAllAsync();
                ViewBag.Features = await _featureRepository.GetAllAsync();
                ViewBag.UnitImages = unit.UnitImages?.ToList() ?? new List<UnitImage>();
                ViewBag.SelectedFeatureIds = selectedFeatureIds ?? new List<int>();
                return View(unit);
            }
        }
    }
}