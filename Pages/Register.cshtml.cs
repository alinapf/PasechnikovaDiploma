using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
    public class RegisterModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly ILogger<RegisterModel> _logger;
        private readonly LogService _logService;

        public RegisterModel(VeterinaryClinicContext context,
                           ILogger<RegisterModel> logger,
                           LogService logService)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
            _logger = logger;
            _logService = logService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Имя пользователя обязательно")]
            [StringLength(50, ErrorMessage = "Не более 50 символов")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Email обязателен")]
            [EmailAddress(ErrorMessage = "Некорректный email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Пароль обязателен")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Минимум 6 символов")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Пароли не совпадают")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync()
        {
            Input = new InputModel();
            await _logService.LogAction(null, "Просмотр страницы регистрации");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                await _logService.LogAction(null,
                    $"Неудачная попытка регистрации: невалидные данные. Ошибки: {errors}");
                return Page();
            }

            await _logService.LogAction(null,
                $"Попытка регистрации с IP: {GetClientIpAddress()}, Логин: {Input.Username}, Email: {Input.Email}");

            var passwordStrength = EvaluatePasswordStrength(Input.Password);
            await _logService.LogAction(null,
                $"Сложность пароля: {passwordStrength}");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (await _context.Users.AnyAsync(u => u.Username == Input.Username))
                {
                    await _logService.LogAction(null,
                        $"Логин '{Input.Username}' уже занят");
                    ModelState.AddModelError("Input.Username", "Логин занят");
                    return Page();
                }

                if (await _context.Users.AnyAsync(u => u.Email == Input.Email))
                {
                    await _logService.LogAction(null,
                        $"Email '{Input.Email}' уже используется");
                    ModelState.AddModelError("Input.Email", "Email уже используется");
                    return Page();
                }

                var user = new User
                {
                    Username = Input.Username,
                    Email = Input.Email,
                    Password = _passwordHasher.HashPassword(null, Input.Password),
                    Role = "Клиент"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var client = new Client
                {
                    UserId = user.UserId,
                    Name = "",
                    Phone = ""
                };

                _context.Clients.Add(client);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                await _logService.LogAction(user.UserId,
                    $"Успешная регистрация: {user.Username} (ID: {user.UserId})");

                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim("ProfileIncomplete", "true")
            };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                await _logService.LogAction(user.UserId,
                    $"Автоматический вход после регистрации");

                return RedirectToPage("/Profile", new { userId = user.UserId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при регистрации");

                await _logService.LogAction(null,
                    $"Ошибка регистрации: {ex.Message}");

                ModelState.AddModelError(string.Empty, "Произошла ошибка при регистрации");
                return Page();
            }
        }

        private string EvaluatePasswordStrength(string password)
        {
            if (password.Length < 6) return "Слишком короткий";
            if (!password.Any(char.IsDigit)) return "Нет цифр";
            if (!password.Any(char.IsUpper)) return "Нет заглавных букв";
            if (!password.Any(ch => !char.IsLetterOrDigit(ch))) return "Нет спецсимволов";
            return "Сильный";
        }

        private string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}
