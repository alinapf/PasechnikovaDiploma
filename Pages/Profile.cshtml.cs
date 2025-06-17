using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VeterinaryClinic.Models;
using VeterinaryClinic.Services;
using static VeterinaryClinic.Models.User;

namespace VeterinaryClinic.Pages
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly LogService _logService;

        public ProfileModel(VeterinaryClinicContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        [BindProperty]
        public UserProfileViewModel UserProfile { get; set; }

        public async Task<IActionResult> OnGetAsync(int userId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                await _logService.LogAction(currentUserId,
                    $"Просмотр профиля пользователя ID: {userId}");

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    await _logService.LogAction(currentUserId,
                        $"Попытка просмотра несуществующего профиля ID: {userId}");
                    return NotFound();
                }

                if (user.Role == "Клиент")
                {
                    var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);

                    if (client == null)
                    {
                        await _logService.LogAction(currentUserId,
                            $"Не найден клиент для пользователя ID: {userId}");
                        return NotFound();
                    }

                    UserProfile = new UserProfileViewModel
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        Email = user.Email,
                        ClientName = client.Name,
                        Phone = client.Phone,
                        Address = client.Address,
                        PetName = client.PetName,
                        PetType = client.PetType
                    };

                    bool isComplete = !string.IsNullOrWhiteSpace(client.Name)
                                   && !string.IsNullOrWhiteSpace(client.Phone)
                                   && !string.IsNullOrWhiteSpace(client.PetName)
                                   && !string.IsNullOrWhiteSpace(client.PetType);

                    if (isComplete)
                    {
                        var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim("ProfileIncomplete", "false")
                    };

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                        await _logService.LogAction(currentUserId,
                            $"Профиль пользователя ID: {userId} помечен как полный");
                    }

                    return Page();
                }

                UserProfile = new UserProfileViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email
                };

                return Page();
            }
            catch (Exception ex)
            {
                await _logService.LogAction(null,
                    $"Ошибка при просмотре профиля: {ex.Message}");
                throw;
            }
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            await _logService.LogAction(currentUserId,
                "Выход пользователя из системы");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostAsync(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (!ModelState.IsValid)
            {
                await _logService.LogAction(currentUserId,
                    $"Неудачная попытка обновления профиля: невалидная модель");
                return Page();
            }

            try
            {
                var user = await _context.Users.FindAsync(userId);
                var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);

                if (user == null || client == null)
                {
                    await _logService.LogAction(currentUserId,
                        $"Попытка обновления несуществующего профиля ID: {userId}");
                    return NotFound();
                }

                var changes = new List<string>();

                // Обновление данных пользователя
                if (!string.IsNullOrWhiteSpace(UserProfile.Email) && user.Email != UserProfile.Email)
                {
                    changes.Add($"Email: {user.Email} → {UserProfile.Email}");
                    user.Email = UserProfile.Email;
                }

                // Обновление данных клиента
                if (!string.IsNullOrWhiteSpace(UserProfile.ClientName) && client.Name != UserProfile.ClientName)
                {
                    changes.Add($"Имя клиента: {client.Name} → {UserProfile.ClientName}");
                    client.Name = UserProfile.ClientName;
                }

                if (!string.IsNullOrWhiteSpace(UserProfile.Phone) && client.Phone != UserProfile.Phone)
                {
                    changes.Add($"Телефон: {client.Phone} → {UserProfile.Phone}");
                    client.Phone = UserProfile.Phone;
                }

                if (!string.IsNullOrWhiteSpace(UserProfile.Address) && client.Address != UserProfile.Address)
                {
                    changes.Add($"Адрес: {client.Address} → {UserProfile.Address}");
                    client.Address = UserProfile.Address;
                }

                if (!string.IsNullOrWhiteSpace(UserProfile.PetName) && client.PetName != UserProfile.PetName)
                {
                    changes.Add($"Имя питомца: {client.PetName} → {UserProfile.PetName}");
                    client.PetName = UserProfile.PetName;
                }

                if (!string.IsNullOrWhiteSpace(UserProfile.PetType) && client.PetType != UserProfile.PetType)
                {
                    changes.Add($"Тип питомца: {client.PetType} → {UserProfile.PetType}");
                    client.PetType = UserProfile.PetType;
                }

                if (changes.Any())
                {
                    await _context.SaveChangesAsync();
                    await _logService.LogAction(currentUserId,
                        $"Обновление профиля ID: {userId}. Изменения: {string.Join("; ", changes)}");
                }
                else
                {
                    await _logService.LogAction(currentUserId,
                        $"Попытка обновления профиля ID: {userId} без изменений");
                }

                return RedirectToPage(new { userId = userId });
            }
            catch (Exception ex)
            {
                await _logService.LogAction(currentUserId,
                    $"Ошибка при обновлении профиля ID: {userId}: {ex.Message}");
                ModelState.AddModelError("", "Произошла ошибка при сохранении данных: " + ex.Message);
                return Page();
            }
        }
    }
}
