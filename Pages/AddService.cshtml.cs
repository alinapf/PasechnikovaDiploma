using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    [Authorize(Roles = "Админ")]
    public class AddServiceModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly LogService _logService;

        public AddServiceModel(
            VeterinaryClinicContext context,
            LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        [BindProperty]
        public Service Service { get; set; } = new Service { ServiceId = 0 };

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            try
            {
                if (id == null)
                {
                    await _logService.LogAction(currentUserId,
                        "Открыта страница создания новой услуги");
                    return Page();
                }

                await _logService.LogAction(currentUserId,
                    $"Открыта страница редактирования услуги ID: {id}");

                Service = await _context.Services
                    .Where(s => s.ServiceId == id)
                    .Select(s => new Service
                    {
                        ServiceId = s.ServiceId,
                        Name = s.Name,
                        Description = s.Description,
                        DurationMinutes = s.DurationMinutes,
                        Price = s.Price,
                        Specialization = s.Specialization
                    })
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (Service == null)
                {
                    await _logService.LogAction(currentUserId,
                        $"Попытка редактирования несуществующей услуги ID: {id}");
                    return NotFound();
                }

                await _logService.LogAction(currentUserId,
                    $"Редактирование услуги: {Service.Name} (ID: {Service.ServiceId})");

                return Page();
            }
            catch (Exception ex)
            {
                await _logService.LogAction(currentUserId,
                    $"Ошибка при открытии страницы услуги: {ex.Message}");
                throw;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (!ModelState.IsValid)
            {
                await _logService.LogAction(currentUserId,
                    "Неудачная попытка сохранения услуги: невалидная модель");
                return Page();
            }

            try
            {
                if (Service.ServiceId == 0)
                {
                    // Создание новой услуги
                    var newService = new Service
                    {
                        Name = Service.Name,
                        Description = Service.Description,
                        Specialization = Service.Specialization,
                        DurationMinutes = Service.DurationMinutes,
                        Price = Service.Price
                    };

                    _context.Services.Add(newService);
                    await _context.SaveChangesAsync();

                    var logMessage = $"Создана новая услуга: " +
                                   $"ID: {newService.ServiceId}, " +
                                   $"Название: {newService.Name}, " +
                                   $"Специализация: {newService.Specialization}, " +
                                   $"Цена: {newService.Price}, " +
                                   $"Длительность: {newService.DurationMinutes} мин.";

                    await _logService.LogAction(currentUserId, logMessage);
                }
                else
                {
                    // Обновление существующей услуги
                    var existingService = await _context.Services.FindAsync(Service.ServiceId);
                    if (existingService == null)
                    {
                        await _logService.LogAction(currentUserId,
                            $"Попытка обновления несуществующей услуги ID: {Service.ServiceId}");
                        return NotFound();
                    }

                    var changes = new List<string>();

                    // Сравниваем и логируем изменения
                    if (existingService.Name != Service.Name)
                    {
                        changes.Add($"Название: {existingService.Name} → {Service.Name}");
                        existingService.Name = Service.Name;
                    }

                    if (existingService.Description != Service.Description)
                    {
                        changes.Add("Описание изменено");
                        existingService.Description = Service.Description;
                    }

                    if (existingService.Specialization != Service.Specialization)
                    {
                        changes.Add($"Специализация: {existingService.Specialization} → {Service.Specialization}");
                        existingService.Specialization = Service.Specialization;
                    }

                    if (existingService.DurationMinutes != Service.DurationMinutes)
                    {
                        changes.Add($"Длительность: {existingService.DurationMinutes} → {Service.DurationMinutes} мин.");
                        existingService.DurationMinutes = Service.DurationMinutes;
                    }

                    if (existingService.Price != Service.Price)
                    {
                        changes.Add($"Цена: {existingService.Price} → {Service.Price}");
                        existingService.Price = Service.Price;
                    }

                    if (changes.Any())
                    {
                        await _context.SaveChangesAsync();

                        var logMessage = $"Обновлена услуга ID: {Service.ServiceId}. " +
                                         $"Изменения: {string.Join(", ", changes)}";

                        await _logService.LogAction(currentUserId, logMessage);
                    }
                    else
                    {
                        await _logService.LogAction(currentUserId,
                            $"Попытка обновления услуги ID: {Service.ServiceId} без изменений");
                    }
                }

                return RedirectToPage("./Services");
            }
            catch (DbUpdateException ex)
            {
                await _logService.LogAction(currentUserId,
                    $"Ошибка базы данных при сохранении услуги: {ex.InnerException?.Message ?? ex.Message}");

                ModelState.AddModelError("",
                    $"Ошибка при сохранении: {(ex.InnerException?.Message ?? ex.Message)}");
                return Page();
            }
            catch (Exception ex)
            {
                await _logService.LogAction(currentUserId,
                    $"Неожиданная ошибка при работе с услугой: {ex.Message}");
                throw;
            }
        }
    }
}
