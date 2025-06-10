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
            // ��������� ������ ���������� ������
            Reviews = await _context.Reviews
                .Include(r => r.Client)
                .Include(r => r.Doctor)
                .Where(r => r.Status == "��������") // ������ �� �������
                .OrderByDescending(r => r.CreatedAt) // ���������� �� ����
                .Select(r => new ReviewViewModel
                {
                    ReviewId = r.ReviewId,
                    ClientName = r.Client.Name,
                    DoctorName = r.Doctor.Name,
                    Comment = r.Comment,
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt // ��������� ����
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
                return NotFound("������ �� ������");
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == SelectedDoctorId);
            if (doctor == null)
            {
                return NotFound("���� �� ������");
            }

            var review = new Review
            {
                ClientId = client.ClientId,
                DoctorId = doctor.DoctorId,
                Comment = comment,
                Rating = Rating,
                CreatedAt = DateTime.Now, // ������������� ������� ���� � �����
                Status = "�� ���������" // ������������� ������ �� ���������
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}

