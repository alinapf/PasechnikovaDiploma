using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly LogService _logService;
        private readonly IPasswordHasher<User> _passwordHasher;

        [BindProperty]
        public ChangePasswordViewModel PasswordModel { get; set; }

        public ChangePasswordModel(
            VeterinaryClinicContext context,
            LogService logService,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _logService = logService;
            _passwordHasher = passwordHasher;
        }

        public void OnGet()
        {
            // Логирование открытия страницы смены пароля
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            _logService.LogAction(currentUserId, "Открытие страницы смены пароля").Wait();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (!ModelState.IsValid)
            {
                await _logService.LogAction(currentUserId,
                    "Неудачная попытка смены пароля: невалидная модель формы");
                return Page();
            }

            try
            {
                var user = await _context.Users.FindAsync(currentUserId);
                if (user == null)
                {
                    await _logService.LogAction(currentUserId,
                        "Попытка смены пароля для несуществующего пользователя");
                    return NotFound();
                }

                // Проверка текущего пароля
                var verificationResult = _passwordHasher.VerifyHashedPassword(
                    user, user.Password, PasswordModel.CurrentPassword);

                if (verificationResult == PasswordVerificationResult.Failed)
                {
                    await _logService.LogAction(currentUserId,
                        "Неудачная попытка смены пароля: неверный текущий пароль");
                    ModelState.AddModelError("", "Текущий пароль неверен");
                    return Page();
                }

                // Хеширование нового пароля
                user.Password = _passwordHasher.HashPassword(user, PasswordModel.NewPassword);
                await _context.SaveChangesAsync();

                await _logService.LogAction(currentUserId,
                    "Пароль пользователя успешно изменен");

                // Добавляем сообщение об успехе
                TempData["SuccessMessage"] = "Пароль успешно изменен";

                return RedirectToPage("/Profile", new { userId = currentUserId });
            }
            catch (Exception ex)
            {
                await _logService.LogAction(currentUserId,
                    $"Ошибка при смене пароля: {ex.Message}");
                ModelState.AddModelError("", "Произошла ошибка при смене пароля: " + ex.Message);
                return Page();
            }
        }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Текущий пароль обязателен")]
        [DataType(DataType.Password)]
        [Display(Name = "Текущий пароль")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Новый пароль обязателен")]
        [DataType(DataType.Password)]
        [StringLength(255, MinimumLength = 8, ErrorMessage = "Пароль должен содержать минимум 8 символов")]
        [Display(Name = "Новый пароль")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
        [Display(Name = "Подтвердите новый пароль")]
        public string ConfirmNewPassword { get; set; }
    }
}
