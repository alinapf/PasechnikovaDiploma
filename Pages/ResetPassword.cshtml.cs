using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using VeterinaryClinic.Models;

namespace VeterinaryClinic.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public ResetPasswordModel(VeterinaryClinicContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Code { get; set; }

        [BindProperty]
        public string NewPassword { get; set; }

        [BindProperty]
        public string ConfirmPassword { get; set; }

        public IActionResult OnGet(string email, string code)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
            {
                return RedirectToPage("/Error");
            }

            Email = email;
            Code = code;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (NewPassword != ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Пароли не совпадают.");
                return Page();
            }

            // Проверяем код восстановления
            var resetCode = await _context.PasswordResetCodes
                .FirstOrDefaultAsync(rc =>
                    rc.Email == Email &&
                    rc.Code == Code &&
                    !rc.IsUsed &&
                    rc.ExpirationDate > DateTime.UtcNow);

            if (resetCode == null)
            {
                ModelState.AddModelError(string.Empty, "Неверный или просроченный код восстановления.");
                return Page();
            }

            // Находим пользователя
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Пользователь не найден.");
                return Page();
            }

            // Обновляем пароль
            user.Password = _passwordHasher.HashPassword(user, NewPassword);

            // Помечаем код как использованный
            resetCode.IsUsed = true;

            await _context.SaveChangesAsync();

            return RedirectToPage("./ResetPasswordConfirmation");
        }
    }
}
