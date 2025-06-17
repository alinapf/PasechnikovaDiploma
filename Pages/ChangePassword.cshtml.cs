using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly LogService _logService;
        private readonly IPasswordHasher<User> _passwordHasher;

        [BindProperty]
        public ChangePasswordViewModel PasswordModel { get; set; }

        public ChangePasswordModel(
            VeterinaryClinicContext context,
            LogService logService,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _logService = logService;
            _passwordHasher = passwordHasher;
        }

        public void OnGet()
        {
            // ����������� �������� �������� ����� ������
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            _logService.LogAction(currentUserId, "�������� �������� ����� ������").Wait();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (!ModelState.IsValid)
            {
                await _logService.LogAction(currentUserId,
                    "��������� ������� ����� ������: ���������� ������ �����");
                return Page();
            }

            try
            {
                var user = await _context.Users.FindAsync(currentUserId);
                if (user == null)
                {
                    await _logService.LogAction(currentUserId,
                        "������� ����� ������ ��� ��������������� ������������");
                    return NotFound();
                }

                // �������� �������� ������
                var verificationResult = _passwordHasher.VerifyHashedPassword(
                    user, user.Password, PasswordModel.CurrentPassword);

                if (verificationResult == PasswordVerificationResult.Failed)
                {
                    await _logService.LogAction(currentUserId,
                        "��������� ������� ����� ������: �������� ������� ������");
                    ModelState.AddModelError("", "������� ������ �������");
                    return Page();
                }

                // ����������� ������ ������
                user.Password = _passwordHasher.HashPassword(user, PasswordModel.NewPassword);
                await _context.SaveChangesAsync();

                await _logService.LogAction(currentUserId,
                    "������ ������������ ������� �������");

                // ��������� ��������� �� ������
                TempData["SuccessMessage"] = "������ ������� �������";

                return RedirectToPage("/Profile", new { userId = currentUserId });
            }
            catch (Exception ex)
            {
                await _logService.LogAction(currentUserId,
                    $"������ ��� ����� ������: {ex.Message}");
                ModelState.AddModelError("", "��������� ������ ��� ����� ������: " + ex.Message);
                return Page();
            }
        }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "������� ������ ����������")]
        [DataType(DataType.Password)]
        [Display(Name = "������� ������")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "����� ������ ����������")]
        [DataType(DataType.Password)]
        [StringLength(255, MinimumLength = 8, ErrorMessage = "������ ������ ��������� ������� 8 ��������")]
        [Display(Name = "����� ������")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "������������� ������ �����������")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "������ �� ���������")]
        [Display(Name = "����������� ����� ������")]
        public string ConfirmNewPassword { get; set; }
    }
}
