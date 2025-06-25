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
                        .Include(a => a.Service)
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

                        // Получаем доступное время, включая текущее время записи
                        AvailableTimes = await GetAvailableTimesAsync(Date, appointment.DoctorId, appointment.ServiceId ?? 0);

                        // Добавляем текущее время записи, если его нет в списке доступных
                        var currentTimeStr = appointment.Time.ToString(@"hh\:mm");
                        if (!AvailableTimes.Contains(currentTimeStr))
                        {
                            AvailableTimes.Add(currentTimeStr);
                            AvailableTimes = AvailableTimes.OrderBy(t => TimeSpan.Parse(t)).ToList();
                        }

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
                // 1. Проверка доступности времени с учетом длительности услуги
                var service = await _context.Services.FindAsync(SelectedServiceId);
                var serviceDuration = service?.DurationMinutes ?? 30;
                var endTime = SelectedTime.Add(TimeSpan.FromMinutes(serviceDuration));

                // Проверяем рабочие часы врача
                var schedule = await _context.DoctorSchedules
                    .FirstOrDefaultAsync(s => s.DoctorId == SelectedDoctorId && s.WorkDate == Date.Date);

                if (schedule == null)
                {
                    ModelState.AddModelError("", "Врач не работает в выбранную дату");
                    await ReloadPageData(currentUserId);
                    return Page();
                }

                var workHours = JsonSerializer.Deserialize<List<WorkHour>>(schedule.WorkHours);
                var isWithinWorkingHours = workHours.Any(wh =>
                    !wh.IsBreak &&
                    SelectedTime >= TimeSpan.Parse(wh.Start) &&
                    endTime <= TimeSpan.Parse(wh.End));

                if (!isWithinWorkingHours)
                {
                    ModelState.AddModelError("", "Выбранное время выходит за рамки рабочего дня врача");
                    await ReloadPageData(currentUserId);
                    return Page();
                }

                // Проверяем конфликты с другими записями
                var result = await _context.Appointments
                    .Where(a => a.DoctorId == SelectedDoctorId &&
                               a.Date.Date == Date.Date &&
                               a.AppointmentId != (IsEditMode ? AppointmentId : null))
                    .Select(a => new
                    {
                        Appointment = a,
                        Duration = a.Service != null ? a.Service.DurationMinutes : 30
                    })
                    .ToListAsync();

                var conflictingAppointments = result
                    .Where(x =>
                    {
                        var appointmentEnd = x.Appointment.Time.Add(TimeSpan.FromMinutes(x.Duration));
                        return x.Appointment.Time < endTime && appointmentEnd > SelectedTime;
                    })
                    .Select(x => x.Appointment)
                    .ToList();

                if (conflictingAppointments.Any())
                {
                    var conflictTimes = string.Join(", ",
                        conflictingAppointments.Select(a => $"{a.Time:hh\\:mm}-{a.Time.Add(TimeSpan.FromMinutes(a.Service.DurationMinutes)):hh\\:mm}"));

                    ModelState.AddModelError("",
                        $"Выбранное время пересекается с другими записями: {conflictTimes}");
                    await ReloadPageData(currentUserId);
                    return Page();
                }

                // 2. Общая валидация модели
                if (!ModelState.IsValid)
                {
                    await _logService.LogAction(currentUserId,
                        "Неудачная попытка сохранения записи: невалидная модель");
                    await ReloadPageData(currentUserId);
                    return Page();
                }

                // 3. Обработка редактирования существующей записи
                if (IsEditMode && AppointmentId.HasValue)
                {
                    return await HandleEditMode(currentUserId);
                }

                // 4. Обработка создания новой записи
                return await HandleCreateMode(currentUserId);
            }
            catch (Exception ex)
            {
                await _logService.LogAction(currentUserId,
                    $"Ошибка при сохранении записи: {ex.Message}");
                ModelState.AddModelError("", "Произошла ошибка при сохранении записи");
                await ReloadPageData(currentUserId);
                return Page();
            }
        }

        private async Task<IActionResult> HandleEditMode(int currentUserId)
        {
            var appointment = await _context.Appointments.FindAsync(AppointmentId.Value);
            if (appointment == null)
            {
                await _logService.LogAction(currentUserId,
                    $"Попытка редактирования несуществующей записи ID: {AppointmentId.Value}");
                return NotFound();
            }

            var changes = new List<string>();

            if (appointment.DoctorId != SelectedDoctorId)
                changes.Add($"Врач: {appointment.DoctorId} → {SelectedDoctorId}");
            if (appointment.ClientId != SelectedClientId)
                changes.Add($"Клиент: {appointment.ClientId} → {SelectedClientId}");
            if (appointment.Date != Date)
                changes.Add($"Дата: {appointment.Date:d} → {Date:d}");
            if (appointment.Time != SelectedTime)
                changes.Add($"Время: {appointment.Time:hh\\:mm} → {SelectedTime:hh\\:mm}");
            if (appointment.ServiceId != SelectedServiceId)
                changes.Add($"Услуга: {appointment.ServiceId} → {SelectedServiceId}");
            if (appointment.Status != Status)
                changes.Add($"Статус: {appointment.Status} → {Status}");

            // Обновляем только измененные поля
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

            return RedirectToPage("/Appointment");
        }

        private async Task<IActionResult> HandleCreateMode(int currentUserId)
        {
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == currentUserId);
            if (client != null)
            {
                SelectedClientId = client.ClientId;
            }

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
                $"Создана новая запись ID: {appointment.AppointmentId}. " +
                $"Детали: врач {SelectedDoctorId}, клиент {SelectedClientId}, " +
                $"{Date:d} {SelectedTime:hh\\:mm}, услуга {SelectedServiceId}");

            return RedirectToPage("/Appointment");
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
                    Name = s.Name,
                    DurationMinutes = s.DurationMinutes
                })
                .ToListAsync();
        }

        private async Task<List<string>> GetAvailableTimesAsync(DateTime date, int doctorId, int serviceId = 0, int? excludeAppointmentId = null)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            try
            {
                // Получаем информацию об услуге
                var serviceDuration = TimeSpan.FromMinutes(30); // Дефолтная длительность
                if (serviceId > 0)
                {
                    var service = await _context.Services.FindAsync(serviceId);
                    if (service != null)
                    {
                        serviceDuration = TimeSpan.FromMinutes(service.DurationMinutes);
                    }
                }

                var availableTimes = new List<string>();
                var schedule = await _context.DoctorSchedules
                    .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.WorkDate == date.Date);

                if (schedule == null)
                {
                    return availableTimes;
                }

                var workHours = JsonSerializer.Deserialize<List<WorkHour>>(schedule.WorkHours);
                var appointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId &&
                               a.Date.Date == date.Date &&
                               (excludeAppointmentId == null || a.AppointmentId != excludeAppointmentId))
                    .Select(a => new { a.Time, a.ServiceId })
                    .ToListAsync();

                // Создаем список занятых интервалов с учетом длительности услуг
                var busyIntervals = new List<(TimeSpan Start, TimeSpan End)>();
                foreach (var app in appointments)
                {
                    var appDuration = TimeSpan.FromMinutes(30); // Дефолтная длительность
                    if (app.ServiceId.HasValue)
                    {
                        var appService = await _context.Services.FindAsync(app.ServiceId.Value);
                        if (appService != null)
                        {
                            appDuration = TimeSpan.FromMinutes(appService.DurationMinutes);
                        }
                    }
                    busyIntervals.Add((app.Time, app.Time.Add(appDuration)));
                }

                foreach (var workHour in workHours.Where(wh => !wh.IsBreak))
                {
                    var workStart = TimeSpan.Parse(workHour.Start);
                    var workEnd = TimeSpan.Parse(workHour.End);

                    var currentSlotStart = workStart;
                    while (currentSlotStart.Add(serviceDuration) <= workEnd)
                    {
                        var currentSlotEnd = currentSlotStart.Add(serviceDuration);

                        // Проверяем, не пересекается ли текущий слот с занятыми интервалами
                        bool isAvailable = true;
                        foreach (var busy in busyIntervals)
                        {
                            if (currentSlotStart < busy.End && currentSlotEnd > busy.Start)
                            {
                                isAvailable = false;
                                break;
                            }
                        }

                        if (isAvailable)
                        {
                            availableTimes.Add(currentSlotStart.ToString(@"hh\:mm"));
                        }

                        currentSlotStart = currentSlotStart.Add(TimeSpan.FromMinutes(30));
                    }
                }

                return availableTimes;
            }
            catch (Exception ex)
            {
                await _logService.LogAction(currentUserId,
                    $"Ошибка при получении доступного времени: {ex.Message}");
                return new List<string>();
            }
        }

        // Обновленный обработчик HTTP-запроса
        public async Task<IActionResult> OnGetAvailableTimes(DateTime date, int doctorId, int serviceId = 0)
        {
            var availableTimes = await GetAvailableTimesAsync(date, doctorId, serviceId);
            return new JsonResult(availableTimes);
        }

        // Обновленный метод ReloadPageData
        private async Task ReloadPageData(int currentUserId)
        {
            Doctors = await GetDoctorsAsync();
            Clients = await GetClientsAsync();
            Services = await GetServicesAsync(SelectedDoctorId);

            // Загружаем доступное время для выбранных параметров
            if (SelectedDoctorId > 0 && Date != default)
            {
                AvailableTimes = await GetAvailableTimesAsync(Date, SelectedDoctorId, SelectedServiceId);
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

    
