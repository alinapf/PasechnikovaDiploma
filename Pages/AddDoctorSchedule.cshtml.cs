using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    public class TimeInterval
    {
        public string Start { get; set; }
        public string End { get; set; }
        public bool IsBreak { get; set; } // ��������� ��� ����������� ���� ���������
    }
    [Authorize(Roles = "�����")]
    public class AddDoctorScheduleModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly LogService _logService;

        public AddDoctorScheduleModel(
            VeterinaryClinicContext context,
            LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        [BindProperty]
        public DoctorScheduleViewModel ViewModel { get; set; }

        [BindProperty]
        public string EventsJson { get; set; }

        public async Task<IActionResult> OnGetAsync(int? doctorId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            try
            {
                if (doctorId == null)
                {
                    await _logService.LogAction(currentUserId,
                        "������� �������� �������� ���������� ��� �������� ID �����");
                    return NotFound();
                }

                var doctor = await _context.Doctors.FindAsync(doctorId);
                if (doctor == null)
                {
                    await _logService.LogAction(currentUserId,
                        $"������� �������� ���������� ��� ��������������� ����� ID: {doctorId}");
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
                        var intervals = JsonSerializer.Deserialize<List<TimeInterval>>(s.WorkHours);
                        return intervals.Select(i => new ScheduleEvent
                        {
                            Title = i.IsBreak ? "�������" : "�����",
                            Start = DateTime.Parse($"{s.WorkDate:yyyy-MM-dd}T{i.Start}"),
                            End = DateTime.Parse($"{s.WorkDate:yyyy-MM-dd}T{i.End}"),
                            Color = i.IsBreak ? "#dc3545" : "#28a745",
                            IsBreak = i.IsBreak
                        });
                    }).ToList()
                };

                await _logService.LogAction(currentUserId,
                    $"�������� ���������� �����: {doctor.Name} (ID: {doctorId})");

                return Page();
            }
            catch (Exception ex)
            {
                await _logService.LogAction(currentUserId,
                    $"������ ��� �������� ���������� ����� ID: {doctorId}: {ex.Message}");
                throw;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            try
            {
                var events = JsonSerializer.Deserialize<List<ScheduleEvent>>(EventsJson);

                // ����������� ����� �����������
                await _logService.LogAction(currentUserId,
                    $"������ ���������� ���������� ��� ����� ID: {ViewModel.DoctorId}. " +
                    $"���������� �������: {events?.Count ?? 0}");

                // ����������� � ��������� �����
                foreach (var e in events)
                {
                    e.Start = DateTime.SpecifyKind(e.Start, DateTimeKind.Utc).ToLocalTime();
                    e.End = DateTime.SpecifyKind(e.End, DateTimeKind.Utc).ToLocalTime();
                }

                // ���������� �� ����
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

                // ������� ������ ������
                var oldSchedules = await _context.DoctorSchedules
                    .Where(s => s.DoctorId == ViewModel.DoctorId)
                    .ToListAsync();

                if (oldSchedules.Any())
                {
                    await _logService.LogAction(currentUserId,
                        $"�������� ������� ���������� ����� ID: {ViewModel.DoctorId}. " +
                        $"���������� ��������� ����: {oldSchedules.Count}");

                    _context.DoctorSchedules.RemoveRange(oldSchedules);
                }

                // ��������� ����� ������
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

                // ��������� ����������� ������������ ����������
                var scheduleInfo = string.Join(", ", grouped.Select(g =>
                    $"{g.Date:dd.MM.yyyy} ({g.Intervals.Count} ����������)"));

                await _logService.LogAction(currentUserId,
                    $"�������� ���������� ���������� ��� ����� ID: {ViewModel.DoctorId}. " +
                    $"���: {scheduleInfo}");

                return RedirectToPage(new { doctorId = ViewModel.DoctorId });
            }
            catch (Exception ex)
            {
                await _logService.LogAction(currentUserId,
                    $"������ ��� ���������� ���������� ����� ID: {ViewModel.DoctorId}: {ex.Message}");

                ModelState.AddModelError("", $"������ ��� ����������: {ex.Message}");
                return Page();
            }
        }
    }

}
