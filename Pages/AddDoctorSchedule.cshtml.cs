using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using System.Globalization;
using System.Text.Json;
using VeterinaryClinic.Models;

namespace VeterinaryClinic.Pages
{
    public class TimeInterval
    {
        public string Start { get; set; }
        public string End { get; set; }
        public bool IsBreak { get; set; } // Добавлено для определения типа интервала
    }
    public class AddDoctorScheduleModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;

        public AddDoctorScheduleModel(VeterinaryClinicContext context)
        {
            _context = context;
        }

        [BindProperty]
        public DoctorScheduleViewModel ViewModel { get; set; }

        [BindProperty]
        public string EventsJson { get; set; }

        public async Task<IActionResult> OnGetAsync(int? doctorId)
        {
            if (doctorId == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null)
            {
                return NotFound();
            }

            var specialSchedules = await _context.DoctorSchedules
                .Where(s => s.DoctorId == doctorId)
                .ToListAsync();

            ViewModel = new DoctorScheduleViewModel
            {
                DoctorId = doctorId.Value,
                DoctorName = doctor.Name,
                Events = specialSchedules.SelectMany(s =>
                {
                    // Десериализация work_hours из JSON
                    var intervals = JsonSerializer.Deserialize<List<TimeInterval>>(s.WorkHours);
                    return intervals.Select(i => new ScheduleEvent
                    {
                        Title = i.IsBreak ? "Перерыв" : "Прием",
                        Start = DateTime.Parse($"{s.WorkDate:yyyy-MM-dd}T{i.Start}"),
                        End = DateTime.Parse($"{s.WorkDate:yyyy-MM-dd}T{i.End}"),
                        Color = i.IsBreak ? "#dc3545" : "#28a745",
                        IsBreak = i.IsBreak
                    });
                }).ToList()
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var events = JsonSerializer.Deserialize<List<ScheduleEvent>>(EventsJson);

                // Преобразуем в локальное время
                foreach (var e in events)
                {
                    e.Start = DateTime.SpecifyKind(e.Start, DateTimeKind.Utc).ToLocalTime();
                    e.End = DateTime.SpecifyKind(e.End, DateTimeKind.Utc).ToLocalTime();
                }

                // Группируем по дате
                var grouped = events
                    .GroupBy(e => e.Start.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Intervals = g.Select(e => new
                        {
                            Start = e.Start.ToString("HH:mm"),
                            End = e.End.ToString("HH:mm"),
                            IsBreak = e.IsBreak
                        }).ToList()
                    });

                // Удаляем старые записи
                var oldSchedules = await _context.DoctorSchedules
                    .Where(s => s.DoctorId == ViewModel.DoctorId)
                    .ToListAsync();
                _context.DoctorSchedules.RemoveRange(oldSchedules);

                // Добавляем новые
                foreach (var day in grouped)
                {
                    var json = JsonSerializer.Serialize(day.Intervals);

                    _context.DoctorSchedules.Add(new DoctorSchedule
                    {
                        DoctorId = ViewModel.DoctorId,
                        WorkDate = day.Date,
                        WorkHours = json
                    });
                }

                await _context.SaveChangesAsync();
                return RedirectToPage(new { doctorId = ViewModel.DoctorId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при сохранении: {ex.Message}");
                return Page();
            }
        }
    }

}
