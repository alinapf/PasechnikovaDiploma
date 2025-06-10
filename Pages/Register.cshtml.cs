using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using VeterinaryClinic.Models;

namespace VeterinaryClinic.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(VeterinaryClinicContext context,
                           ILogger<RegisterModel> logger)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "��� ������������ �����������")]
            [StringLength(50, ErrorMessage = "�� ����� 50 ��������")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Email ����������")]
            [EmailAddress(ErrorMessage = "������������ email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "������ ����������")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "������� 6 ��������")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "������ �� ���������")]
            public string ConfirmPassword { get; set; }
        }

        public void OnGet()
        {
            Input = new InputModel();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (await _context.Users.AnyAsync(u => u.Username == Input.Username))
                {
                    ModelState.AddModelError("Input.Username", "����� �����");
                    return Page();
                }

                if (await _context.Users.AnyAsync(u => u.Email == Input.Email))
                {
                    ModelState.AddModelError("Input.Email", "Email ��� ������������");
                    return Page();
                }

                var user = new User
                {
                    Username = Input.Username,
                    Email = Input.Email,
                    Password = _passwordHasher.HashPassword(null, Input.Password),
                    Role = "������"
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

                // ���� ������������
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

                // ��������������� �� �������
                return RedirectToPage("/Profile", new { userId = user.UserId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "������ ��� �����������");
                ModelState.AddModelError(string.Empty, "��������� ������ ��� �����������");
                return Page();
            }
        }
    }
}
