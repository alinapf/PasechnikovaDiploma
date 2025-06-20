using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly IEmailSender _emailSender;
        private readonly LogService _logService;

        public ForgotPasswordModel(VeterinaryClinicContext context,
                                 IEmailSender emailSender,
                                 LogService logService)
        {
            _context = context;
            _emailSender = emailSender;
            _logService = logService;
        }

        [BindProperty]
        public string Email { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(Email))
                {
                    await _logService.LogAction(null,
                        $"Попытка восстановления пароля: не указан email");
                    ModelState.AddModelError(string.Empty, "Введите адрес электронной почты.");
                    return Page();
                }

                // Логируем факт попытки восстановления пароля (без указания email в логах)
                await _logService.LogAction(null,
                    $"Запрос на восстановление пароля для email (анонимизировано): {AnonymizeEmail(Email)}");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
                if (user == null)
                {
                    // Логируем факт, что пользователь не найден (без указания email)
                    await _logService.LogAction(null,
                        "Попытка восстановления пароля: пользователь с указанным email не найден");

                    // Не сообщаем, что пользователь не найден (из соображений безопасности)
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // Генерируем код восстановления
                var code = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

                // Сохраняем код в базу данных
                var resetCode = new PasswordResetCode
                {
                    Email = Email,
                    Code = code,
                    ExpirationDate = DateTime.UtcNow.AddHours(1),
                    IsUsed = false
                };

                _context.PasswordResetCodes.Add(resetCode);
                await _context.SaveChangesAsync();

                // Логируем создание кода восстановления (без указания самого кода)
                await _logService.LogAction(user.UserId,
                    $"Сгенерирован код восстановления пароля для пользователя ID: {user.UserId}");

                // Отправляем email
                var callbackUrl = Url.Page(
                    "/ResetPassword",
                    pageHandler: null,
                    values: new { email = Email, code = code },
                    protocol: Request.Scheme);

                var emailBody = $"Для восстановления пароля используйте следующий код: {code}\n" +
                               $"Или перейдите по ссылке: <a href='{callbackUrl}'>восстановить пароль</a>";

                await _emailSender.SendEmailAsync(
                    Email,
                    "Восстановление пароля",
                    emailBody);

                // Логируем отправку email (без указания email и содержимого)
                await _logService.LogAction(user.UserId,
                    $"Отправлено письмо для восстановления пароля пользователю ID: {user.UserId}");

                return RedirectToPage("./ForgotPasswordConfirmation", new { email = Email });
            }
            catch (Exception ex)
            {
                await _logService.LogAction(null,
                    $"Ошибка при обработке запроса восстановления пароля: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Произошла ошибка при обработке запроса.");
                return Page();
            }
        }

        private string AnonymizeEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                return "invalid-email";

            var parts = email.Split('@');
            var name = parts[0];
            var domain = parts[1];

            if (name.Length > 2)
                return $"{name[0]}...{name[^1]}@{domain}";

            return $"...@{domain}";
        }
    }
}
