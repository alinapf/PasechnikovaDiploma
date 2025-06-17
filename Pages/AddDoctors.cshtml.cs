using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using System;
using System.Security.Claims;
using System.Text;
using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    public class AddDoctorsModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly LogService _logService;

        public AddDoctorsModel(VeterinaryClinicContext context,
                             IWebHostEnvironment environment,
                             IPasswordHasher<User> passwordHasher,
                             LogService logService)
        {
            _context = context;
            _environment = environment;
            _passwordHasher = passwordHasher;
            _logService = logService;
        }

        [BindProperty]
        public Doctor Doctor { get; set; }

        [BindProperty]
        public IFormFile PhotoFile { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            id ??= HttpContext.Request.Query["doctorId"].Count > 0
                ? int.Parse(HttpContext.Request.Query["doctorId"])
                : null;

            try
            {
                if (id.HasValue)
                {
                    Doctor = await _context.Doctors
                        .Include(d => d.User)
                        .FirstOrDefaultAsync(d => d.DoctorId == id);

                    if (Doctor == null)
                    {
                        await _logService.LogAction(currentUserId,
                            $"Попытка просмотра несуществующего врача ID: {id}");
                        return NotFound();
                    }

                    await _logService.LogAction(currentUserId,
                        $"Просмотр данных врача ID: {id}, Имя: {Doctor.Name}");
                }
                else
                {
                    Doctor = new Doctor
                    {
                        PhotoUrl = "/images/doctors/default.jpg",
                        User = new User()
                    };
                    await _logService.LogAction(currentUserId,
                        "Начато создание нового врача");
                }

                return Page();
            }
            catch (Exception ex)
            {
                await _logService.LogAction(currentUserId,
                    $"Ошибка при просмотре данных врача: {ex.Message}");
                throw;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var action = Doctor.DoctorId == 0 ? "Создание" : "Редактирование";
            var logMessage = new StringBuilder();

            try
            {
                // Обработка фото
                if (PhotoFile != null && PhotoFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "doctors");
                    Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = $"{Guid.NewGuid()}.jpg";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    await using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await PhotoFile.CopyToAsync(fileStream);
                    }

                    // Удаляем старое фото, если оно не дефолтное (при редактировании)
                    if (Doctor.DoctorId != 0 && !string.IsNullOrEmpty(Doctor.PhotoUrl) &&
                        !Doctor.PhotoUrl.Equals("/images/doctors/default.jpg"))
                    {
                        var oldFilePath = Path.Combine(_environment.WebRootPath, Doctor.PhotoUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                            logMessage.AppendLine($"Удалено старое фото: {Doctor.PhotoUrl}");
                        }
                    }

                    Doctor.PhotoUrl = $"/images/doctors/{uniqueFileName}";
                    logMessage.AppendLine($"Загружено новое фото: {Doctor.PhotoUrl}");
                }
                else if (Doctor.DoctorId == 0 && string.IsNullOrEmpty(Doctor.PhotoUrl))
                {
                    Doctor.PhotoUrl = "/images/doctors/default.jpg";
                    logMessage.AppendLine("Установлено фото по умолчанию");
                }

                if (Doctor.DoctorId == 0)
                {
                    // Создаем нового пользователя для врача
                    Doctor.User ??= new User();
                    Doctor.User.Username = GenerateUsername(Doctor.Name);
                    Doctor.User.Email = GenerateEmail(Doctor.Name);
                    Doctor.User.Password = _passwordHasher.HashPassword(null, GenerateDefaultPassword(Doctor.Name));
                    Doctor.User.Role = "Врач";

                    _context.Doctors.Add(Doctor);

                    logMessage.AppendLine($"Создан новый врач: {Doctor.Name}");
                    logMessage.AppendLine($"Специализация: {Doctor.Specialization}");
                    logMessage.AppendLine($"Создан пользователь: {Doctor.User.Username}, Роль: {Doctor.User.Role}");
                }
                else
                {
                    // Для существующего врача обновляем только данные врача
                    var existingDoctor = await _context.Doctors
                        .Include(d => d.User)
                        .FirstOrDefaultAsync(d => d.DoctorId == Doctor.DoctorId);

                    if (existingDoctor != null)
                    {
                        logMessage.AppendLine($"Обновление врача ID: {existingDoctor.DoctorId}");

                        // Логируем изменения
                        if (existingDoctor.Name != Doctor.Name)
                            logMessage.AppendLine($"Имя: {existingDoctor.Name} ? {Doctor.Name}");

                        if (existingDoctor.Specialization != Doctor.Specialization)
                            logMessage.AppendLine($"Специализация: {existingDoctor.Specialization} ? {Doctor.Specialization}");

                        if (existingDoctor.Bio != Doctor.Bio)
                            logMessage.AppendLine($"Биография изменена");

                        // Обновляем поля
                        existingDoctor.Name = Doctor.Name;
                        existingDoctor.Specialization = Doctor.Specialization;
                        existingDoctor.Bio = Doctor.Bio;

                        if (!string.IsNullOrEmpty(Doctor.PhotoUrl))
                        {
                            existingDoctor.PhotoUrl = Doctor.PhotoUrl;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                await _logService.LogAction(currentUserId,
                    $"{action} врача: {Doctor.Name}. Детали: {logMessage}");

                return RedirectToPage("./Doctors");
            }
            catch (Exception ex)
            {
                await _logService.LogAction(currentUserId,
                    $"Ошибка при {action.ToLower()} врача: {ex.Message}. Данные: {Doctor.Name}, {Doctor.Specialization}");
                ModelState.AddModelError("", "Произошла ошибка при сохранении данных врача");
                return Page();
            }
        }

        private string GenerateUsername(string name)
        {
            return name.Replace(" ", "").ToLower();
        }

        private string GenerateEmail(string name)
        {
            return $"{name.Replace(" ", ".").ToLower()}@clinic.com";
        }

        private string GenerateDefaultPassword(string name)
        {
            return $"{name.Split(' ')[0]}@123";
        }
    }
}

