using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

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
                await _logService.LogAction(null,
                    $"��������� ������� ������ ������ ��� {Email}: ���������� ������");
                return Page();
            }

            if (NewPassword != ConfirmPassword)
            {
                await _logService.LogAction(null,
                    $"��������� ������� ������ ������ ��� {Email}: ������ �� ���������");
                ModelState.AddModelError(string.Empty, "������ �� ���������.");
                return Page();
            }

            // ��������� ��� ��������������
            var resetCode = await _context.PasswordResetCodes
                .FirstOrDefaultAsync(rc =>
                    rc.Email == Email &&
                    rc.Code == Code &&
                    !rc.IsUsed &&
                    rc.ExpirationDate > DateTime.UtcNow);

            if (resetCode == null)
            {
                await _logService.LogAction(null,
                    $"��������� ������� ������ ������ ��� {Email}: �������� ��� ������������ ���");
                ModelState.AddModelError(string.Empty, "�������� ��� ������������ ��� ��������������.");
                return Page();
            }

            // ������� ������������
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (user == null)
            {
                await _logService.LogAction(null,
                    $"��������� ������� ������ ������: ������������ � email {Email} �� ������");
                ModelState.AddModelError(string.Empty, "������������ �� ������.");
                return Page();
            }

            // �������� ����� ���������� ������
            await _logService.LogAction(user.UserId,
                $"����������� ����� ������ ��� ������������ {user.Email} (ID: {user.UserId})");

            // ��������� ������
            user.Password = _passwordHasher.HashPassword(user, NewPassword);

            // �������� ��� ��� ��������������
            resetCode.IsUsed = true;

            await _context.SaveChangesAsync();

            // �������� �������� ���������
            await _logService.LogAction(user.UserId,
                $"������ ������� ������� ��� ������������ {user.Email} (ID: {user.UserId})");

            return RedirectToPage("./ResetPasswordConfirmation");
        }
    }
}
