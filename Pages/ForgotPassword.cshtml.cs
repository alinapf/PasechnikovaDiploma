using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly IEmailSender _emailSender;
        private readonly LogService _logService;

        public ForgotPasswordModel(VeterinaryClinicContext context,
                                 IEmailSender emailSender,
                                 LogService logService)
        {
            _context = context;
            _emailSender = emailSender;
            _logService = logService;
        }

        [BindProperty]
        public string Email { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(Email))
                {
                    await _logService.LogAction(null,
                        $"������� �������������� ������: �� ������ email");
                    ModelState.AddModelError(string.Empty, "������� ����� ����������� �����.");
                    return Page();
                }

                // �������� ���� ������� �������������� ������ (��� �������� email � �����)
                await _logService.LogAction(null,
                    $"������ �� �������������� ������ ��� email (���������������): {AnonymizeEmail(Email)}");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
                if (user == null)
                {
                    // �������� ����, ��� ������������ �� ������ (��� �������� email)
                    await _logService.LogAction(null,
                        "������� �������������� ������: ������������ � ��������� email �� ������");

                    // �� ��������, ��� ������������ �� ������ (�� ����������� ������������)
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // ���������� ��� ��������������
                var code = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

                // ��������� ��� � ���� ������
                var resetCode = new PasswordResetCode
                {
                    Email = Email,
                    Code = code,
                    ExpirationDate = DateTime.UtcNow.AddHours(1),
                    IsUsed = false
                };

                _context.PasswordResetCodes.Add(resetCode);
                await _context.SaveChangesAsync();

                // �������� �������� ���� �������������� (��� �������� ������ ����)
                await _logService.LogAction(user.UserId,
                    $"������������ ��� �������������� ������ ��� ������������ ID: {user.UserId}");

                // ���������� email
                var callbackUrl = Url.Page(
                    "/ResetPassword",
                    pageHandler: null,
                    values: new { email = Email, code = code },
                    protocol: Request.Scheme);

                var emailBody = $"��� �������������� ������ ����������� ��������� ���: {code}\n" +
                               $"��� ��������� �� ������: <a href='{callbackUrl}'>������������ ������</a>";

                await _emailSender.SendEmailAsync(
                    Email,
                    "�������������� ������",
                    emailBody);

                // �������� �������� email (��� �������� email � �����������)
                await _logService.LogAction(user.UserId,
                    $"���������� ������ ��� �������������� ������ ������������ ID: {user.UserId}");

                return RedirectToPage("./ForgotPasswordConfirmation", new { email = Email });
            }
            catch (Exception ex)
            {
                await _logService.LogAction(null,
                    $"������ ��� ��������� ������� �������������� ������: {ex.Message}");
                ModelState.AddModelError(string.Empty, "��������� ������ ��� ��������� �������.");
                return Page();
            }
        }

        private string AnonymizeEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                return "invalid-email";

            var parts = email.Split('@');
            var name = parts[0];
            var domain = parts[1];

            if (name.Length > 2)
                return $"{name[0]}...{name[^1]}@{domain}";

            return $"...@{domain}";
        }
    }
}
