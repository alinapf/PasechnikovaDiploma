using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    [Authorize]
    public class AppointmentModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly LogService _logService;

        public AppointmentModel(VeterinaryClinicContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFilter { get; set; }

        public List<AppointmentViewModel> Appointments { get; set; }

        public async Task<IActionResult> OnGetAsync(int? userId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var query = _context.Appointments
                    .Include(a => a.Client)
                    .Include(a => a.Doctor)
                    .AsQueryable();

                // ���������� �� ������������ (���� �� �����)
                if (!User.IsInRole("�����") || userId != null)
                {
                    var targetUserId = userId ?? currentUserId;
                    var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == targetUserId);

                    if (client == null)
                    {
                        await _logService.LogAction(currentUserId,
                            $"�� ������ ������ ��� ������������ ID: {targetUserId}");
                        return NotFound();
                    }

                    query = query.Where(a => a.ClientId == client.ClientId);
                }

                // ���������� ������
                if (!string.IsNullOrEmpty(SearchString))
                {
                    query = query.Where(a =>
                        a.Client.Name.Contains(SearchString) ||
                        a.Doctor.Name.Contains(SearchString));
                }

                // ���������� �� ����
                if (DateFilter.HasValue)
                {
                    query = query.Where(a => a.Date.Date == DateFilter.Value.Date);
                }

                Appointments = await query
                    .OrderBy(a => a.Date)
                    .ThenBy(a => a.Time)
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

                // �����������
                var actionDescription = User.IsInRole("�����")
                    ? $"������������� ���������� ������ ������� (������: {(string.IsNullOrEmpty(SearchString) ? "���" : SearchString)}, ����: {(DateFilter.HasValue ? DateFilter.Value.ToShortDateString() : "���")})"
                    : "������������ ���������� ���� ������ �� �����";

                await _logService.LogAction(currentUserId, actionDescription);

                return Page();
            }
            catch (Exception ex)
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                await _logService.LogAction(currentUserId,
                    $"������ ��� ��������� ������ ������� �� �����: {ex.Message}");
                throw;
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Client)
                    .FirstOrDefaultAsync(a => a.AppointmentId == id);

                if (appointment == null)
                {
                    await _logService.LogAction(currentUserId,
                        $"������� �������� �������������� ������ �� ����� ID: {id}");
                    return NotFound();
                }

                // �������� ���� �������
                if (!User.IsInRole("�����"))
                {
                    var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == currentUserId);
                    if (client == null || appointment.ClientId != client.ClientId)
                    {
                        await _logService.LogAction(currentUserId,
                            $"������� �������� ����� ������ �� ����� ID: {id}");
                        return Forbid();
                    }
                }

                var logMessage = $"������� ������ �� ����� ID: {id}, " +
                                $"������: {appointment.Client?.Name}, " +
                                $"����: {appointment.Date.ToShortDateString()}, " +
                                $"�����: {appointment.Time}";

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();

                await _logService.LogAction(currentUserId, logMessage);

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                await _logService.LogAction(currentUserId,
                    $"������ ��� �������� ������ �� ����� ID: {id}: {ex.Message}");
                throw;
            }
        }
    }
}
