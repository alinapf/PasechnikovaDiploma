using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using VeterinaryClinic.Models;

namespace VeterinaryClinic.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(VeterinaryClinicContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [BindProperty]
        public string Email { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email))
            {
                ModelState.AddModelError(string.Empty, "������� ����� ����������� �����.");
                return Page();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (user == null)
            {
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

            return RedirectToPage("./ForgotPasswordConfirmation");
        }
    }
}
