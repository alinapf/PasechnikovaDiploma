using VeterinaryClinic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace VeterinaryClinic.Pages
{
    public class DoctorsModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;

        public DoctorsModel(VeterinaryClinicContext context)
        {
            _context = context;
        }

        public List<DoctorViewModel> Doctors { get; set; }

        public async Task OnGetAsync()
        {
            // Сначала получаем основные данные
            var doctorsData = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Schedules)
                .Select(d => new 
                {
                    Doctor = d,
                    Schedules = d.Schedules,
                    Reviews = _context.Reviews
                        .Where(r => r.DoctorId == d.DoctorId)
                        .Join(_context.Clients,
                            r => r.ClientId,
                            c => c.ClientId,
                            (r, c) => new ReviewViewModel
                            {
                                ClientName = c.Name ?? "Аноним",
                                Comment = r.Comment,
                                Rating = r.Rating
                            })
                        .ToList()
                })
                .ToListAsync();

            // Затем преобразуем в конечную модель
            Doctors = doctorsData.Select(data => new DoctorViewModel
            {
                DoctorId = data.Doctor.DoctorId,
                Name = data.Doctor.Name,
                Specialization = data.Doctor.Specialization,
                Rating = data.Doctor.Rating,
                PhotoUrl = data.Doctor.PhotoUrl,
                Bio = data.Doctor.Bio,
                Schedule = FormatSchedule(data.Schedules),
                Reviews = data.Reviews,
                AvailableTimes = GetAvailableTimes(data.Schedules, DateTime.Today, data.Doctor.DoctorId)
            }).ToList();
        }

        private static string FormatSchedule(List<DoctorSchedule> schedules)
        {
            if (schedules == null || !schedules.Any())
                return "График не указан";

            var days = new Dictionary<int, string>
        {
            {1, "Пн"}, {2, "Вт"}, {3, "Ср"}, {4, "Чт"}, {5, "Пт"}, {6, "Сб"}, {7, "Вс"}
        };

            /*var grouped = schedules
                .Where(s => s.IsWorkingDay == true) // Явное сравнение с true
                .GroupBy(s => new { s.StartTime, s.EndTime })
                .Select(g => new
                {
                    Time = $"{g.Key.StartTime:hh\\:mm}-{g.Key.EndTime:hh\\:mm}",
                    Days = g.Select(x => x.DayOfWeek).OrderBy(x => x).ToList()
                })
                .OrderBy(x => x.Days.FirstOrDefault()) // Используем FirstOrDefault для безопасности
                .ToList();*/

            var result = new List<string>();
            /*foreach (var group in grouped)
            {
                var dayRanges = new List<string>();
                int currentStart = (int)group.Days[0];
                var prev = currentStart;

                for (int i = 1; i < group.Days.Count; i++)
                {
                    if (group.Days[i] != prev + 1)
                    {
                        dayRanges.Add(currentStart == prev
                            ? days[currentStart]
                            : $"{days[currentStart]}-{days[prev]}");
                        currentStart = (int)group.Days[i];
                    }
                    prev = (int)group.Days[i];
                }

                dayRanges.Add(currentStart == prev
                    ? days[currentStart]
                    : $"{days[currentStart]}-{days[prev]}");

                result.Add($"{string.Join(",", dayRanges)} {group.Time}");
            }*/

            return string.Join("; ", result);
        }

        private List<string> GetAvailableTimes(List<DoctorSchedule> schedules, DateTime date, int doctorId)
        {
            var availableTimes = new List<string>();

            var dayOfWeek = (int)date.DayOfWeek; // Получаем день недели (0 = Вс, 1 = Пн, ..., 6 = Сб)

            /*var doctorSchedules = schedules
                .Where(s => s.DoctorId == doctorId &&
                           s.DayOfWeek.HasValue &&
                           s.DayOfWeek.Value == dayOfWeek &&
                           s.IsWorkingDay.HasValue &&
                           s.IsWorkingDay.Value)
                .ToList();*/

            /*foreach (var schedule in doctorSchedules)
            {
                for (var time = schedule.StartTime; time < schedule.EndTime; time = time.Add(TimeSpan.FromMinutes(30))) // Интервал в 30 минут
                {
                    var isBooked = _context.Appointments
                        .Any(a => a.Date.Date == date.Date && a.Time == time && a.DoctorId == doctorId);
                    if (!isBooked)
                    {
                        availableTimes.Add(time.ToString(@"hh\:mm"));
                    }
                }
            }*/

            return availableTimes;
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }

            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }

}
