using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly LogService _logService;

        public ResetPasswordModel(VeterinaryClinicContext context, LogService logService)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
            _logService = logService;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Введите код восстановления")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Код должен содержать 6 символов")]
        public string Code { get; set; }

        [BindProperty]
        public string NewPassword { get; set; }

        [BindProperty]
        public string ConfirmPassword { get; set; }
        public string DisplayEmail { get; set; }
        public IActionResult OnGet(string email, string code)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToPage("/Error");
            }

            Email = email;
            DisplayEmail = AnonymizeEmail(email);
            if (!string.IsNullOrEmpty(code) && code.Length == 6)
            {
                Code = code;
            }

            return Page();
        }
        private string AnonymizeEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                return email;

            var parts = email.Split('@');
            if (parts[0].Length <= 3)
                return $"{parts[0].Substring(0, 1)}***@{parts[1]}";

            return $"{parts[0].Substring(0, 3)}***@{parts[1]}";
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (NewPassword != ConfirmPassword)
            {
                await _logService.LogAction(null,
                    $"Неудачная попытка сброса пароля для {Email}: пароли не совпадают");
                ModelState.AddModelError(string.Empty, "Пароли не совпадают.");
                return Page();
            }

            // Проверяем код восстановления (с учетом регистра и пробелов)
            var resetCode = await _context.PasswordResetCodes
                .FirstOrDefaultAsync(rc =>
                    rc.Email == Email &&
                    rc.Code.Trim() == Code.Trim() && // Удаляем возможные пробелы
                    !rc.IsUsed &&
                    rc.ExpirationDate > DateTime.UtcNow);

            if (resetCode == null)
            {
                await _logService.LogAction(null,
                    $"Неудачная попытка сброса пароля для {Email}: неверный или просроченный код");
                ModelState.AddModelError(string.Empty, "Неверный или просроченный код восстановления.");
                return Page();
            }

            // Находим пользователя
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (user == null)
            {
                await _logService.LogAction(null,
                    $"Неудачная попытка сброса пароля: пользователь с email {Email} не найден");
                ModelState.AddModelError(string.Empty, "Пользователь не найден.");
                return Page();
            }

            // Логируем перед изменением пароля
            await _logService.LogAction(user.UserId,
                $"Инициирован сброс пароля для пользователя {user.Email} (ID: {user.UserId})");

            // Обновляем пароль
            user.Password = _passwordHasher.HashPassword(user, NewPassword);

            // Помечаем код как использованный
            resetCode.IsUsed = true;

            await _context.SaveChangesAsync();

            // Логируем успешное изменение
            await _logService.LogAction(user.UserId,
                $"Пароль успешно изменен для пользователя {user.Email} (ID: {user.UserId})");

            return RedirectToPage("./ResetPasswordConfirmation");
        }
    }
}
