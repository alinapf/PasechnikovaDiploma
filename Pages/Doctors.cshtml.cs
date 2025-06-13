using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using VeterinaryClinic.Models;

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
            // Get doctor data with schedules and reviews
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

            // Convert to view model
            Doctors = doctorsData.Select(data => new DoctorViewModel
            {
                DoctorId = data.Doctor.DoctorId,
                Name = data.Doctor.Name,
                Specialization = data.Doctor.Specialization,
                Rating = data.Doctor.Rating,
                PhotoUrl = data.Doctor.PhotoUrl,
                Bio = data.Doctor.Bio,
                Schedule = FormatWeeklySchedule(data.Schedules),
                Reviews = data.Reviews,
                AvailableTimes = GetAvailableTimes(data.Schedules, DateTime.Today, data.Doctor.DoctorId)
            }).ToList();
        }

        private static string FormatWeeklySchedule(List<DoctorSchedule> schedules)
        {
            if (schedules == null || !schedules.Any())
                return "График не указан";

            var dayNames = new Dictionary<DayOfWeek, string>
    {
        {DayOfWeek.Monday, "Пн"},
        {DayOfWeek.Tuesday, "Вт"},
        {DayOfWeek.Wednesday, "Ср"},
        {DayOfWeek.Thursday, "Чт"},
        {DayOfWeek.Friday, "Пт"},
        {DayOfWeek.Saturday, "Сб"},
        {DayOfWeek.Sunday, "Вс"}
    };

            // Получаем даты текущей недели (с понедельника по воскресенье)
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
                    result.Add($"{dayName} - выходной");
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
                        result.Add($"{dayName} - выходной");
                        continue;
                    }

                    var timeSlots = workingSlots
                        .Select(wh => $"{wh.Start}-{wh.End}")
                        .ToList();

                    result.Add($"{dayName} - {string.Join(", ", timeSlots)}");
                }
                catch
                {
                    result.Add($"{dayName} - ошибка расписания");
                }
            }

            return string.Join("\n", result);
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

    // Helper class for JSON deserialization
    public class WorkHour
    {
        public string Start { get; set; }
        public string End { get; set; }
        public bool IsBreak { get; set; }
    }

}
