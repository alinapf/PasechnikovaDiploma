using VeterinaryClinic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace VeterinaryClinic.Pages
{
    public class IndexModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;

        public IndexModel(VeterinaryClinicContext context)
        {
            _context = context;
        }

        public List<ReviewViewModel> Reviews { get; set; }
        public List<DoctorViewModel> Doctors { get; set; } = new List<DoctorViewModel>();

        public async Task OnGetAsync()
        {
            // Загружаем только одобренные отзывы
            Reviews = await _context.Reviews
                .Include(r => r.Client)
                .Include(r => r.Doctor)
                .Where(r => r.Status == "Одобрено") // Фильтр по статусу
                .OrderByDescending(r => r.CreatedAt) // Сортировка по дате
                .Select(r => new ReviewViewModel
                {
                    ReviewId = r.ReviewId,
                    ClientName = r.Client.Name,
                    DoctorName = r.Doctor.Name,
                    Comment = r.Comment,
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt // Добавляем дату
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
        }

        public async Task<IActionResult> OnPostSubmitReviewAsync(string comment, int Rating, int SelectedDoctorId)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null)
            {
                return NotFound("Клиент не найден");
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == SelectedDoctorId);
            if (doctor == null)
            {
                return NotFound("Врач не найден");
            }

            var review = new Review
            {
                ClientId = client.ClientId,
                DoctorId = doctor.DoctorId,
                Comment = comment,
                Rating = Rating,
                CreatedAt = DateTime.Now, // Устанавливаем текущую дату и время
                Status = "На модерации" // Устанавливаем статус по умолчанию
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}

