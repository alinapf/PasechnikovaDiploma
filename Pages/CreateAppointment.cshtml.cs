using VeterinaryClinic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using System.Security.Claims;

namespace VeterinaryClinic.Pages
{
    public class CreateAppointmentModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;

        public CreateAppointmentModel(VeterinaryClinicContext context)
        {
            _context = context;
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
            Doctors = await GetDoctorsAsync();
            Clients = await GetClientsAsync();
            Services = new List<ServiceViewModel>();

            if (id.HasValue)
            {
                // Режим редактирования
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
                }
            }
            else
            {
                // Режим создания новой записи
                if (!User.IsInRole("Админ"))
                {
                    // Для обычных пользователей автоматически выбираем текущего клиента
                    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                    var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
                    if (client != null)
                    {
                        SelectedClientId = client.ClientId;
                    }
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Doctors = await GetDoctorsAsync();
                Clients = await GetClientsAsync();
                Services = await GetServicesAsync(SelectedDoctorId);
                return Page();
            }

            if (IsEditMode && AppointmentId.HasValue)
            {
                // Режим редактирования
                var appointment = await _context.Appointments.FindAsync(AppointmentId.Value);
                if (appointment != null)
                {
                    appointment.DoctorId = SelectedDoctorId;
                    appointment.ClientId = SelectedClientId;
                    appointment.Date = Date;
                    appointment.Time = SelectedTime;
                    appointment.ServiceId = SelectedServiceId;
                    appointment.Status = Status;
                    await _context.SaveChangesAsync();
                    return RedirectToPage("/Appointment");
                }
            }
            else
            {
                // Режим создания новой записи
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
            }

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
            var dayOfWeek = (byte)date.DayOfWeek;

            // Получаем расписание врача на этот день недели
           /* var schedule = await _context.DoctorSchedules
                .FirstOrDefaultAsync(s => s.DoctorId == doctorId &&
                                        s.DayOfWeek == dayOfWeek &&
                                        (s.IsWorkingDay == true)); // Явное сравнение с true*/

            var availableTimes = new List<string>();


            /*if (schedule == null)
            {
                // Врач не работает в этот день
                return new JsonResult(availableTimes);
            }
            // Получаем все записи врача на выбранную дату
            var bookedTimes = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.Date.Date == date.Date)
                .Select(a => a.Time)
                .ToListAsync(); 

                var currentTime = schedule.StartTime;
                while (currentTime < schedule.EndTime)
                {
                    if (!bookedTimes.Contains(currentTime))
                    {
                        // Форматируем время, например "09:30"
                        availableTimes.Add(currentTime.ToString(@"hh\:mm"));
                    }
                    currentTime = currentTime.Add(TimeSpan.FromMinutes(30));
                }
*/
            

            return new JsonResult(availableTimes);
        }
    }

}

    
