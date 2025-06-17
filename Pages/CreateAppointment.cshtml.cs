using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text.Json;
using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    public class CreateAppointmentModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly LogService _logService;

        public CreateAppointmentModel(VeterinaryClinicContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        [BindProperty]
        public int? AppointmentId { get; set; }

        [BindProperty]
        public bool IsEditMode { get; set; }

        [BindProperty]
        public DateTime Date { get; set; }

        [BindProperty]
        public TimeSpan SelectedTime { get; set; }

        [BindProperty]
        public int SelectedDoctorId { get; set; }

        [BindProperty]
        public int SelectedClientId { get; set; }

        [BindProperty]
        public int SelectedServiceId { get; set; }

        [BindProperty]
        public string Status { get; set; } = "запланировано";

        public List<string> AvailableTimes { get; set; } = new List<string>();
        public List<DoctorViewModel> Doctors { get; set; } = new List<DoctorViewModel>();
        public List<ClientViewModel> Clients { get; set; } = new List<ClientViewModel>();
        public List<ServiceViewModel> Services { get; set; } = new List<ServiceViewModel>();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                Doctors = await GetDoctorsAsync();
                Clients = await GetClientsAsync();
                Services = new List<ServiceViewModel>();

                if (id.HasValue)
                {
                    await _logService.LogAction(currentUserId,
                        $"Начало редактирования записи ID: {id.Value}");

                    IsEditMode = true;
                    AppointmentId = id.Value;

                    var appointment = await _context.Appointments
                        .Include(a => a.Doctor)
                        .Include(a => a.Client)
                        .FirstOrDefaultAsync(a => a.AppointmentId == id.Value);

                    if (appointment != null)
                    {
                        Date = appointment.Date;
                        SelectedTime = appointment.Time;
                        SelectedDoctorId = appointment.DoctorId;
                        SelectedClientId = appointment.ClientId;
                        SelectedServiceId = appointment.ServiceId ?? 0;
                        Status = appointment.Status;
                        Services = await GetServicesAsync(appointment.DoctorId);

                        await _logService.LogAction(currentUserId,
                            $"Загружены данные записи: врач ID {appointment.DoctorId}, " +
                            $"клиент ID {appointment.ClientId}, дата {appointment.Date.ToShortDateString()}");
                    }
                }
                else
                {
                    await _logService.LogAction(currentUserId, "Начало создания новой записи");

                    if (!User.IsInRole("Админ"))
                    {
                        var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == currentUserId);
                        if (client != null)
                        {
                            SelectedClientId = client.ClientId;
                            await _logService.LogAction(currentUserId,
                                $"Автоматически выбран клиент ID: {client.ClientId} для текущего пользователя");
                        }
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                await _logService.LogAction(null,
                    $"Ошибка при загрузке страницы создания/редактирования записи: {ex.Message}");
                throw;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            try
            {
                if (!ModelState.IsValid)
                {
                    await _logService.LogAction(currentUserId,
                        "Неудачная попытка сохранения записи: невалидная модель");
                    Doctors = await GetDoctorsAsync();
                    Clients = await GetClientsAsync();
                    Services = await GetServicesAsync(SelectedDoctorId);
                    return Page();
                }

                if (IsEditMode && AppointmentId.HasValue)
                {
                    await _logService.LogAction(currentUserId,
                        $"Сохранение изменений записи ID: {AppointmentId.Value}");

                    var appointment = await _context.Appointments.FindAsync(AppointmentId.Value);
                    if (appointment != null)
                    {
                        var changes = new List<string>();

                        if (appointment.DoctorId != SelectedDoctorId)
                            changes.Add($"Врач: {appointment.DoctorId} → {SelectedDoctorId}");
                        if (appointment.ClientId != SelectedClientId)
                            changes.Add($"Клиент: {appointment.ClientId} → {SelectedClientId}");
                        if (appointment.Date != Date)
                            changes.Add($"Дата: {appointment.Date.ToShortDateString()} → {Date.ToShortDateString()}");
                        if (appointment.Time != SelectedTime)
                            changes.Add($"Время: {appointment.Time} → {SelectedTime}");
                        if (appointment.ServiceId != SelectedServiceId)
                            changes.Add($"Услуга: {appointment.ServiceId} → {SelectedServiceId}");
                        if (appointment.Status != Status)
                            changes.Add($"Статус: {appointment.Status} → {Status}");

                        appointment.DoctorId = SelectedDoctorId;
                        appointment.ClientId = SelectedClientId;
                        appointment.Date = Date;
                        appointment.Time = SelectedTime;
                        appointment.ServiceId = SelectedServiceId;
                        appointment.Status = Status;

                        await _context.SaveChangesAsync();

                        if (changes.Any())
                        {
                            await _logService.LogAction(currentUserId,
                                $"Изменения записи ID: {AppointmentId.Value}: {string.Join("; ", changes)}");
                        }
                        else
                        {
                            await _logService.LogAction(currentUserId,
                                $"Попытка сохранения записи ID: {AppointmentId.Value} без изменений");
                        }
                    }
                }
                else
                {
                    var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == currentUserId);
                    if (client != null)
                    {
                        SelectedClientId = client.ClientId;
                    }

                    await _logService.LogAction(currentUserId,
                        $"Создание новой записи: врач ID {SelectedDoctorId}, " +
                        $"клиент ID {SelectedClientId}, дата {Date.ToShortDateString()}, " +
                        $"время {SelectedTime}, услуга ID {SelectedServiceId}");

                    var appointment = new Appointment
                    {
                        ClientId = SelectedClientId,
                        DoctorId = SelectedDoctorId,
                        Date = Date,
                        Time = SelectedTime,
                        Status = Status,
                        ServiceId = SelectedServiceId
                    };

                    _context.Appointments.Add(appointment);
                    await _context.SaveChangesAsync();

                    await _logService.LogAction(currentUserId,
                        $"Создана новая запись ID: {appointment.AppointmentId}");
                }

                return RedirectToPage("/Appointment");
            }
            catch (Exception ex)
            {
                await _logService.LogAction(currentUserId,
                    $"Ошибка при сохранении записи: {ex.Message}");
                ModelState.AddModelError("", "Произошла ошибка при сохранении записи");
                Doctors = await GetDoctorsAsync();
                Clients = await GetClientsAsync();
                Services = await GetServicesAsync(SelectedDoctorId);
                return Page();
            }
        }

        private async Task<List<DoctorViewModel>> GetDoctorsAsync()
        {
            return await _context.Doctors
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

        private async Task<List<ClientViewModel>> GetClientsAsync()
        {
            return await _context.Clients
                .Include(c => c.User)
                .Select(c => new ClientViewModel
                {
                    ClientId = c.ClientId,
                    Name = c.Name,
                    Email = c.User.Email
                })
                .ToListAsync();
        }
        public async Task<IActionResult> OnGetServicesForDoctor(int doctorId)
        {
            var services = await GetServicesAsync(doctorId);
            return new JsonResult(services);
        }
        private async Task<List<ServiceViewModel>> GetServicesAsync(int doctorId)
        {
            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null)
            {
                return new List<ServiceViewModel>();
            }

            return await _context.Services
                .Where(s => s.Specialization == doctor.Specialization)
                .Select(s => new ServiceViewModel
                {
                    ServiceId = s.ServiceId,
                    Name = s.Name
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnGetAvailableTimes(DateTime date, int doctorId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            try
            {
                await _logService.LogAction(currentUserId,
                    $"Запрос доступного времени для врача ID: {doctorId} на дату {date.ToShortDateString()}");

                var availableTimes = new List<string>();
                var schedule = await _context.DoctorSchedules
                    .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.WorkDate == date.Date);

                if (schedule == null)
                {
                    await _logService.LogAction(currentUserId,
                        $"Врач ID: {doctorId} не работает {date.ToShortDateString()}");
                    return new JsonResult(availableTimes);
                }

                var workHours = JsonSerializer.Deserialize<List<WorkHour>>(schedule.WorkHours);
                var bookedTimes = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId && a.Date.Date == date.Date)
                    .Select(a => a.Time)
                    .ToListAsync();

                foreach (var workHour in workHours.Where(wh => !wh.IsBreak))
                {
                    var startTime = TimeSpan.Parse(workHour.Start);
                    var endTime = TimeSpan.Parse(workHour.End);

                    var currentTime = startTime;
                    while (currentTime < endTime)
                    {
                        if (!bookedTimes.Contains(currentTime))
                        {
                            availableTimes.Add(currentTime.ToString(@"hh\:mm"));
                        }
                        currentTime = currentTime.Add(TimeSpan.FromMinutes(30));
                    }
                }

                await _logService.LogAction(currentUserId,
                    $"Найдено {availableTimes.Count} доступных слотов для врача ID: {doctorId}");

                return new JsonResult(availableTimes);
            }
            catch (Exception ex)
            {
                await _logService.LogAction(currentUserId,
                    $"Ошибка при получении доступного времени: {ex.Message}");
                return new JsonResult(new List<string>());
            }
        }

        private class WorkHour
        {
            public string Start { get; set; }
            public string End { get; set; }
            public bool IsBreak { get; set; }
        }
    }

}

    
