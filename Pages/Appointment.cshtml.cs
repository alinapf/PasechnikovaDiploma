using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VeterinaryClinic.Models;

namespace VeterinaryClinic.Pages
{
    public class AppointmentModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;

        public AppointmentModel(VeterinaryClinicContext context)
        {
            _context = context;
        }

        public List<AppointmentViewModel> Appointments { get; set; } // Список записей на прием

        public async Task<IActionResult> OnGetAsync(int? userId)
        {
            if (User.IsInRole("Админ") && userId == null)
            {
                // Для администратора - показываем все записи, если не указан конкретный пользователь
                Appointments = await _context.Appointments
                    .Include(a => a.Client)
                    .Include(a => a.Doctor)
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
            }
            else
            {
                // Для обычных пользователей или когда администратор смотрит конкретного пользователя
                var currentUserId = userId ?? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == currentUserId);

                if (client == null)
                {
                    return NotFound();
                }

                Appointments = await _context.Appointments
                    .Include(a => a.Client)
                    .Include(a => a.Doctor)
                    .Where(a => a.ClientId == client.ClientId)
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
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

    }
}
