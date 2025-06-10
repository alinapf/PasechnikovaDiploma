using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VeterinaryClinic.Models;
using static VeterinaryClinic.Models.User;

namespace VeterinaryClinic.Pages
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;

        public ProfileModel(VeterinaryClinicContext context)
        {
            _context = context;
        }

        [BindProperty]
        public UserProfileViewModel UserProfile { get; set; }


        public async Task<IActionResult> OnGetAsync(int userId)
        {

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();
            if (user.Role == "Клиент")
            {
                var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);

                if (client == null)
                {
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
                    // Обновляем ClaimsPrincipal
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim("ProfileIncomplete", "false") // 👈 Обновляем
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                }

                return Page();
            }
            UserProfile = new UserProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email
            };
;
            return Page();
        }
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostAsync(int userId)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var user = await _context.Users.FindAsync(userId);
                var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);

                if (user == null || client == null)
                {
                    return NotFound();
                }

                // Обновление данных пользователя
                if (!string.IsNullOrWhiteSpace(UserProfile.Email))
                {
                    user.Email = UserProfile.Email;
                }

                // Обновление данных клиента
                if (!string.IsNullOrWhiteSpace(UserProfile.ClientName))
                {
                    client.Name = UserProfile.ClientName;
                }

                if (!string.IsNullOrWhiteSpace(UserProfile.Phone))
                {
                    client.Phone = UserProfile.Phone;
                }

                if (!string.IsNullOrWhiteSpace(UserProfile.Address))
                {
                    client.Address = UserProfile.Address;
                }

                if (!string.IsNullOrWhiteSpace(UserProfile.PetName))
                {
                    client.PetName = UserProfile.PetName;
                }

                if (!string.IsNullOrWhiteSpace(UserProfile.PetType))
                {
                    client.PetType = UserProfile.PetType;
                }

                await _context.SaveChangesAsync();

                // PRG pattern чтобы избежать повторной отправки формы
                return RedirectToPage(new { userId = userId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Произошла ошибка при сохранении данных: " + ex.Message);
                return Page();
            }
        }
    }
}
