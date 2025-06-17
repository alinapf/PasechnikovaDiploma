using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    [Authorize]
    public class AppointmentModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly LogService _logService;

        public AppointmentModel(VeterinaryClinicContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFilter { get; set; }

        public List<AppointmentViewModel> Appointments { get; set; }

        public async Task<IActionResult> OnGetAsync(int? userId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var query = _context.Appointments
                    .Include(a => a.Client)
                    .Include(a => a.Doctor)
                    .AsQueryable();

                // Фильтрация по пользователю (если не админ)
                if (!User.IsInRole("Админ") || userId != null)
                {
                    var targetUserId = userId ?? currentUserId;
                    var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == targetUserId);

                    if (client == null)
                    {
                        await _logService.LogAction(currentUserId,
                            $"Не найден клиент для пользователя ID: {targetUserId}");
                        return NotFound();
                    }

                    query = query.Where(a => a.ClientId == client.ClientId);
                }

                // Применение поиска
                if (!string.IsNullOrEmpty(SearchString))
                {
                    query = query.Where(a =>
                        a.Client.Name.Contains(SearchString) ||
                        a.Doctor.Name.Contains(SearchString));
                }

                // Фильтрация по дате
                if (DateFilter.HasValue)
                {
                    query = query.Where(a => a.Date.Date == DateFilter.Value.Date);
                }

                Appointments = await query
                    .OrderBy(a => a.Date)
                    .ThenBy(a => a.Time)
                    .Select(a => new AppointmentViewModel
                    {
                        AppointmentId = a.AppointmentId,
                        ClientName = a.Client.Name,
                        DoctorName = a.Doctor.Name,
                        Date = a.Date,
                        Time = a.Time,
                        Status = a.Status
                    })
                    .ToListAsync();

                // Логирование
                var actionDescription = User.IsInRole("Админ")
                    ? $"Администратор просмотрел список записей (фильтр: {(string.IsNullOrEmpty(SearchString) ? "нет" : SearchString)}, дата: {(DateFilter.HasValue ? DateFilter.Value.ToShortDateString() : "все")})"
                    : "Пользователь просмотрел свои записи на прием";

                await _logService.LogAction(currentUserId, actionDescription);

                return Page();
            }
            catch (Exception ex)
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                await _logService.LogAction(currentUserId,
                    $"Ошибка при получении списка записей на прием: {ex.Message}");
                throw;
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Client)
                    .FirstOrDefaultAsync(a => a.AppointmentId == id);

                if (appointment == null)
                {
                    await _logService.LogAction(currentUserId,
                        $"Попытка удаления несуществующей записи на прием ID: {id}");
                    return NotFound();
                }

                // Проверка прав доступа
                if (!User.IsInRole("Админ"))
                {
                    var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == currentUserId);
                    if (client == null || appointment.ClientId != client.ClientId)
                    {
                        await _logService.LogAction(currentUserId,
                            $"Попытка удаления чужой записи на прием ID: {id}");
                        return Forbid();
                    }
                }

                var logMessage = $"Удалена запись на прием ID: {id}, " +
                                $"Клиент: {appointment.Client?.Name}, " +
                                $"Дата: {appointment.Date.ToShortDateString()}, " +
                                $"Время: {appointment.Time}";

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();

                await _logService.LogAction(currentUserId, logMessage);

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                await _logService.LogAction(currentUserId,
                    $"Ошибка при удалении записи на прием ID: {id}: {ex.Message}");
                throw;
            }
        }
    }
}
