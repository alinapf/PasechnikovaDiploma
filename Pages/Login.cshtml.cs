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
            [Required(ErrorMessage = "������� �����")]
            public string Username { get; set; }

            [Required(ErrorMessage = "������� ������")]
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
                        $"��������� ������� �����: ���������� ������ ��� ������������ {Input.Username}");
                    return Page();
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == Input.Username);
                if (user == null)
                {
                    await _logService.LogAction(null,
                        $"������� ����� � �������������� �������: {Input.Username}");
                    ModelState.AddModelError("", "�������� ����� ��� ������");
                    return Page();
                }

                var result = _passwordHasher.VerifyHashedPassword(user, user.Password, Input.Password);
                if (result == PasswordVerificationResult.Failed)
                {
                    await _logService.LogAction(user.UserId,
                        $"��������� ������� ����� ��� ������������ {user.Username} (ID: {user.UserId}): �������� ������");
                    ModelState.AddModelError("", "�������� ����� ��� ������");
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
                    $"�������� ���� ������������ {user.Username} (ID: {user.UserId}) � ����� {user.Role}");

                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                await _logService.LogAction(null,
                    $"������ ��� ������� ����� ��� ������������ {Input?.Username}: {ex.Message}");
                ModelState.AddModelError("", "��������� ������ ��� ����� � �������");
                return Page();
            }
        }
    }

}