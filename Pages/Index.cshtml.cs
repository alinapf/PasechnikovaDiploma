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
                // ����������� ������ �������� ������� ��������
                await _logService.LogAction(null, "�������� ������� ��������: ������");

                // ��������� ������ ���������� ������
                Reviews = await _context.Reviews
                    .Include(r => r.Client)
                    .Include(r => r.Doctor)
                    .Where(r => r.Status == "��������")
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

                // ����������� �������� �������� ������
                await _logService.LogAction(null,
                    $"�������� ������� �������� ���������. ��������� �������: {Reviews.Count}, ������: {Doctors.Count}");
            }
            catch (Exception ex)
            {
                await _logService.LogAction(null,
                    $"������ ��� �������� ������� ��������: {ex.Message}");
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
                        "������� �������� ������: ���������� ������");
                    return Page();
                }

                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    await _logService.LogAction(null,
                        "������� �������� ������: ������������ �� �����������");
                    return Unauthorized();
                }

                if (!int.TryParse(userIdString, out int userId))
                {
                    await _logService.LogAction(null,
                        "������� �������� ������: �������� ������ ID ������������");
                    return Unauthorized();
                }

                var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
                if (client == null)
                {
                    await _logService.LogAction(userId,
                        "������� �������� ������: ������ �� ������");
                    return NotFound("������ �� ������");
                }

                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == SelectedDoctorId);
                if (doctor == null)
                {
                    await _logService.LogAction(userId,
                        $"������� �������� ������: ���� � ID {SelectedDoctorId} �� ������");
                    return NotFound("���� �� ������");
                }

                var review = new Review
                {
                    ClientId = client.ClientId,
                    DoctorId = doctor.DoctorId,
                    Comment = comment,
                    Rating = Rating,
                    CreatedAt = DateTime.Now,
                    Status = "�� ���������"
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                await _logService.LogAction(userId,
                    $"��������� ����� ����� ��� ����� {doctor.Name} (ID: {doctor.DoctorId}). " +
                    $"�������: {Rating}, ������: �� ���������");

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                await _logService.LogAction(null,
                    $"������ ��� �������� ������: {ex.Message}");
                ModelState.AddModelError("", "��������� ������ ��� �������� ������");
                return Page();
            }
        }
    }
}

