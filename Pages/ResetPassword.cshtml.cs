using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
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
        [Required(ErrorMessage = "������� ��� ��������������")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "��� ������ ��������� 6 ��������")]
        public string Code { get; set; }

        [BindProperty]
        public string NewPassword { get; set; }

        [BindProperty]
        public string ConfirmPassword { get; set; }
        public string DisplayEmail { get; set; }
        public IActionResult OnGet(string email, string code)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToPage("/Error");
            }

            Email = email;
            DisplayEmail = AnonymizeEmail(email);
            if (!string.IsNullOrEmpty(code) && code.Length == 6)
            {
                Code = code;
            }

            return Page();
        }
        private string AnonymizeEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                return email;

            var parts = email.Split('@');
            if (parts[0].Length <= 3)
                return $"{parts[0].Substring(0, 1)}***@{parts[1]}";

            return $"{parts[0].Substring(0, 3)}***@{parts[1]}";
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (NewPassword != ConfirmPassword)
            {
                await _logService.LogAction(null,
                    $"��������� ������� ������ ������ ��� {Email}: ������ �� ���������");
                ModelState.AddModelError(string.Empty, "������ �� ���������.");
                return Page();
            }

            // ��������� ��� �������������� (� ������ �������� � ��������)
            var resetCode = await _context.PasswordResetCodes
                .FirstOrDefaultAsync(rc =>
                    rc.Email == Email &&
                    rc.Code.Trim() == Code.Trim() && // ������� ��������� �������
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
