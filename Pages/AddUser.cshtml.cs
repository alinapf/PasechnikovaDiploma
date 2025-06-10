using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using VeterinaryClinic.Models;

namespace VeterinaryClinic.Pages
{
    public class AddUserModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;

        public AddUserModel(VeterinaryClinicContext context)
        {
            _context = context;
        }

        [BindProperty]
        public User User { get; set; } = new User { UserId = 0 };

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                // ����� �������� ������ ������������
                return Page();
            }

            // ����� �������������� ������������� ������������
            User = await _context.Users.FindAsync(id);

            if (User == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (User.UserId == 0)
            {
                // ���������� ������ ������������
                _context.Users.Add(User);
            }
            else
            {
                // ���������� ������������� ������������
                var userToUpdate = await _context.Users.FindAsync(User.UserId);

                if (userToUpdate == null)
                {
                    return NotFound();
                }

                userToUpdate.Username = User.Username;
                userToUpdate.Email = User.Email;
                userToUpdate.Role = User.Role;

                // ��������� ������ ������ ���� �� ��� ������
                if (!string.IsNullOrEmpty(User.Password))
                {
                    userToUpdate.Password = User.Password;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "�� ������� ��������� ���������. " + ex.Message);
                return Page();
            }

            return RedirectToPage("./Users");
        }
    }
}
