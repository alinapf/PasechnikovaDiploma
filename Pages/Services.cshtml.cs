using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    public class ServiceModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly LogService _logService;

        public ServiceModel(VeterinaryClinicContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        public IList<Service> Services { get; set; } = new List<Service>();

        public async Task OnGetAsync()
        {
            Services = await _context.Services
                .Select(a => new Service
                {
                    ServiceId = a.ServiceId,
                    Name = a.Name,
                    Description = a.Description,
                    DurationMinutes = a.DurationMinutes,
                    Price = a.Price,
                    Specialization = a.Specialization
                }).ToListAsync();
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            // Получаем ID текущего пользователя (кто выполняет удаление)
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            // Логируем действие
            await _logService.LogAction(
                currentUserId,
                $"Удалена услуга: {service.Name} (ID: {service.ServiceId}, Цена: {service.Price} руб.)");

            return RedirectToPage();
        }
    }
}
