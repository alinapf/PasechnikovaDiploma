using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    public class DoctorsModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly LogService _logService;

        public DoctorsModel(VeterinaryClinicContext context,
                           IWebHostEnvironment environment,
                           LogService logService)
        {
            _context = context;
            _environment = environment;
            _logService = logService;
        }

        public List<DoctorViewModel> Doctors { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                await _logService.LogAction(null, "������ �������� �������� ������");

                // Get doctor data with schedules and reviews
                var doctorsData = await _context.Doctors
                    .Include(d => d.User)
                    .Include(d => d.Schedules)
                    .ToListAsync();

                var doctorsList = new List<DoctorViewModel>();

                foreach (var doctor in doctorsData)
                {
                    // �������� ��� ������ ��� ����� (������� �� ���������)
                    var allReviews = await _context.Reviews
                        .Where(r => r.DoctorId == doctor.DoctorId)
                        .Join(_context.Clients,
                            r => r.ClientId,
                            c => c.ClientId,
                            (r, c) => new ReviewViewModel
                            {
                                ReviewId = r.ReviewId,
                                ClientName = c.Name ?? "������",
                                Comment = r.Comment,
                                Rating = r.Rating,
                                CreatedAt = r.CreatedAt,
                                Status = r.Status
                            })
                        .ToListAsync();

                    // ������������ ������� ������� �� ���� �������
                    if (allReviews.Any())
                    {
                        double newRating = allReviews.Average(r => r.Rating);
                        doctor.Rating = (int)Math.Round(newRating);

                        // ��������� ������� ����� � ���� ������
                        _context.Doctors.Update(doctor);
                        await _context.SaveChangesAsync();
                    }

                    // ��������� ������ - ������ ����������
                    var approvedReviews = allReviews
                        .Where(r => r.Status == "��������")
                        .ToList();

                    // �������� ���������� �����
                    var schedules = await _context.DoctorSchedules
                        .Where(s => s.DoctorId == doctor.DoctorId)
                        .ToListAsync();

                    doctorsList.Add(new DoctorViewModel
                    {
                        DoctorId = doctor.DoctorId,
                        Name = doctor.Name,
                        Specialization = doctor.Specialization,
                        Rating = doctor.Rating,
                        PhotoUrl = doctor.PhotoUrl,
                        Bio = doctor.Bio,
                        Schedule = FormatWeeklySchedule(schedules),
                        Reviews = approvedReviews,
                        AvailableTimes = GetAvailableTimes(schedules, DateTime.Today, doctor.DoctorId)
                    });
                }

                Doctors = doctorsList;

                await _logService.LogAction(null,
                    $"�������� �������� �������� ������. ��������� ������: {Doctors.Count}");
            }
            catch (Exception ex)
            {
                await _logService.LogAction(null,
                    $"������ ��� �������� �������� ������: {ex.Message}");
                throw;
            }
        }

        private static string FormatWeeklySchedule(List<DoctorSchedule> schedules)
        {
            if (schedules == null || !schedules.Any())
                return "������ �� ������";

            var dayNames = new Dictionary<DayOfWeek, string>
            {
                {DayOfWeek.Monday, "��"},
                {DayOfWeek.Tuesday, "��"},
                {DayOfWeek.Wednesday, "��"},
                {DayOfWeek.Thursday, "��"},
                {DayOfWeek.Friday, "��"},
                {DayOfWeek.Saturday, "��"},
                {DayOfWeek.Sunday, "��"}
            };

            // �������� ���� ������� ������ (� ������������ �� �����������)
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var weekDates = Enumerable.Range(0, 7)
                .Select(offset => startOfWeek.AddDays(offset))
                .ToList();

            var result = new List<string>();

            foreach (var date in weekDates)
            {
                var daySchedule = schedules.FirstOrDefault(s => s.WorkDate.Date == date.Date);
                var dayName = dayNames[date.DayOfWeek];

                if (daySchedule == null)
                {
                    result.Add($"{dayName} - ��������");
                    continue;
                }

                try
                {
                    var workHours = JsonConvert.DeserializeObject<List<WorkHour>>(daySchedule.WorkHours);
                    var workingSlots = workHours
                        .Where(wh => !wh.IsBreak)
                        .OrderBy(wh => TimeSpan.Parse(wh.Start))
                        .ToList();

                    if (!workingSlots.Any())
                    {
                        result.Add($"{dayName} - ��������");
                        continue;
                    }

                    var timeSlots = workingSlots
                        .Select(wh => $"{wh.Start}-{wh.End}")
                        .ToList();

                    result.Add($"{dayName} - {string.Join(", ", timeSlots)}");
                }
                catch
                {
                    result.Add($"{dayName} - ������ ����������");
                }
            }

            return string.Join("<br>", result);
        }

        private List<string> GetAvailableTimes(List<DoctorSchedule> schedules, DateTime date, int doctorId)
        {
            var availableTimes = new List<string>();

            var daySchedule = schedules.FirstOrDefault(s => s.WorkDate.Date == date.Date);
            if (daySchedule == null)
                return availableTimes;

            try
            {
                var workHours = JsonConvert.DeserializeObject<List<WorkHour>>(daySchedule.WorkHours);
                var workingSlots = workHours.Where(wh => !wh.IsBreak).ToList();

                foreach (var slot in workingSlots)
                {
                    var start = TimeSpan.Parse(slot.Start);
                    var end = TimeSpan.Parse(slot.End);

                    for (var time = start; time < end; time = time.Add(TimeSpan.FromMinutes(30)))
                    {
                        var isBooked = _context.Appointments
                            .Any(a => a.Date.Date == date.Date &&
                                    a.Time == time &&
                                    a.DoctorId == doctorId);

                        if (!isBooked)
                        {
                            availableTimes.Add(time.ToString(@"hh\:mm"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing work hours: {ex.Message}");
            }

            return availableTimes;
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            try
            {
                await _logService.LogAction(currentUserId,
                    $"������� �������� ����� ID: {id}");

                // ������� ����� ������ � ��������� �������������
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.DoctorId == id);

                if (doctor == null)
                {
                    await _logService.LogAction(currentUserId,
                        $"������� �������� ��������������� ����� ID: {id}");
                    return NotFound();
                }

                // �������� ���������� � ����� ����� ���������
                await _logService.LogAction(currentUserId,
                    $"�������� �����: {doctor.Name} (ID: {doctor.DoctorId}), " +
                    $"�������������: {doctor.Specialization}, " +
                    $"����: {doctor.PhotoUrl}");

                // ������� ���� ����� (���� ��� ���������� � �� ���������)
                if (!string.IsNullOrEmpty(doctor.PhotoUrl) &&
                    !doctor.PhotoUrl.Equals("/images/doctors/default.jpg", StringComparison.OrdinalIgnoreCase))
                {
                    var filePath = Path.Combine(_environment.WebRootPath, doctor.PhotoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        await _logService.LogAction(currentUserId,
                            $"������� ���� �����: {filePath}");
                    }
                }

                // ��������� ID ������������ ����� ���������
                var userId = doctor.UserId;

                // ������� �����
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();

                await _logService.LogAction(currentUserId,
                    $"���� ID: {id} ������� ������ �� ���� ������");

                // ������� ���������� ������������
                if (userId > 0)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        await _logService.LogAction(currentUserId,
                            $"�������� ���������� ������������ ID: {userId} ��� ����� ID: {id}");

                        _context.Users.Remove(user);
                        await _context.SaveChangesAsync();

                        await _logService.LogAction(currentUserId,
                            $"������������ ID: {userId} ������� ������");
                    }
                }

                return RedirectToPage("./Doctors");
            }
            catch (Exception ex)
            {
                await _logService.LogAction(currentUserId,
                    $"������ ��� �������� ����� ID: {id}: {ex.Message}");
                return RedirectToPage("./Doctors", new { error = "�� ������� ������� �����" });
            }
        }
    }

    public class WorkHour
    {
        public string Start { get; set; }
        public string End { get; set; }
        public bool IsBreak { get; set; }
    }

}
