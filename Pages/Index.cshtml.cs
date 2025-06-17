using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    public class IndexModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly LogService _logService;

        public IndexModel(VeterinaryClinicContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        public List<ReviewViewModel> Reviews { get; set; }
        public List<DoctorViewModel> Doctors { get; set; } = new List<DoctorViewModel>();

        public async Task OnGetAsync()
        {
            try
            {
                // Логирование начала загрузки главной страницы
                await _logService.LogAction(null, "Загрузка главной страницы: начало");

                // Загружаем только одобренные отзывы
                Reviews = await _context.Reviews
                    .Include(r => r.Client)
                    .Include(r => r.Doctor)
                    .Where(r => r.Status == "Одобрено")
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ReviewViewModel
                    {
                        ReviewId = r.ReviewId,
                        ClientName = r.Client.Name,
                        DoctorName = r.Doctor.Name,
                        Comment = r.Comment,
                        Rating = r.Rating,
                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();

                Doctors = await _context.Doctors
                    .Select(d => new DoctorViewModel
                    {
                        DoctorId = d.DoctorId,
                        Name = d.Name,
                        Specialization = d.Specialization,
                        Rating = d.Rating,
                        PhotoUrl = d.PhotoUrl,
                        Bio = d.Bio,
                    })
                    .ToListAsync();

                // Логирование успешной загрузки данных
                await _logService.LogAction(null,
                    $"Загрузка главной страницы завершена. Загружено отзывов: {Reviews.Count}, врачей: {Doctors.Count}");
            }
            catch (Exception ex)
            {
                await _logService.LogAction(null,
                    $"Ошибка при загрузке главной страницы: {ex.Message}");
                throw;
            }
        }

        public async Task<IActionResult> OnPostSubmitReviewAsync(string comment, int Rating, int SelectedDoctorId)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await _logService.LogAction(null,
                        "Попытка отправки отзыва: невалидная модель");
                    return Page();
                }

                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    await _logService.LogAction(null,
                        "Попытка отправки отзыва: пользователь не авторизован");
                    return Unauthorized();
                }

                if (!int.TryParse(userIdString, out int userId))
                {
                    await _logService.LogAction(null,
                        "Попытка отправки отзыва: неверный формат ID пользователя");
                    return Unauthorized();
                }

                var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
                if (client == null)
                {
                    await _logService.LogAction(userId,
                        "Попытка отправки отзыва: клиент не найден");
                    return NotFound("Клиент не найден");
                }

                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == SelectedDoctorId);
                if (doctor == null)
                {
                    await _logService.LogAction(userId,
                        $"Попытка отправки отзыва: врач с ID {SelectedDoctorId} не найден");
                    return NotFound("Врач не найден");
                }

                var review = new Review
                {
                    ClientId = client.ClientId,
                    DoctorId = doctor.DoctorId,
                    Comment = comment,
                    Rating = Rating,
                    CreatedAt = DateTime.Now,
                    Status = "На модерации"
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                await _logService.LogAction(userId,
                    $"Отправлен новый отзыв для врача {doctor.Name} (ID: {doctor.DoctorId}). " +
                    $"Рейтинг: {Rating}, статус: На модерации");

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                await _logService.LogAction(null,
                    $"Ошибка при отправке отзыва: {ex.Message}");
                ModelState.AddModelError("", "Произошла ошибка при отправке отзыва");
                return Page();
            }
        }
    }
}

