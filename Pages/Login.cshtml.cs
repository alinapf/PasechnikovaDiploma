using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly LogService _logService;

        public LoginModel(VeterinaryClinicContext context, LogService logService)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
            _logService = logService;
        }

        [BindProperty]
        public LoginInputModel Input { get; set; }

        public class LoginInputModel
        {
            [Required(ErrorMessage = "Введите логин")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Введите пароль")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await _logService.LogAction(null,
                        $"Неудачная попытка входа: невалидная модель для пользователя {Input.Username}");
                    return Page();
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == Input.Username);
                if (user == null)
                {
                    await _logService.LogAction(null,
                        $"Попытка входа с несуществующим логином: {Input.Username}");
                    ModelState.AddModelError("", "Неверный логин или пароль");
                    return Page();
                }

                var result = _passwordHasher.VerifyHashedPassword(user, user.Password, Input.Password);
                if (result == PasswordVerificationResult.Failed)
                {
                    await _logService.LogAction(user.UserId,
                        $"Неудачная попытка входа для пользователя {user.Username} (ID: {user.UserId}): неверный пароль");
                    ModelState.AddModelError("", "Неверный логин или пароль");
                    return Page();
                }

                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
            };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                await _logService.LogAction(user.UserId,
                    $"Успешный вход пользователя {user.Username} (ID: {user.UserId}) с ролью {user.Role}");

                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                await _logService.LogAction(null,
                    $"Ошибка при попытке входа для пользователя {Input?.Username}: {ex.Message}");
                ModelState.AddModelError("", "Произошла ошибка при входе в систему");
                return Page();
            }
        }
    }

}