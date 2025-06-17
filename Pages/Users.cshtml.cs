using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    public class UsersModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly LogService _logService;

        public UsersModel(VeterinaryClinicContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        public IList<User> Users { get; set; } = new List<User>();

        public async Task OnGetAsync()
        {
            var users = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                users = users.Where(u =>
                    u.Username.Contains(SearchString) ||
                    u.Email.Contains(SearchString));
            }

            Users = await users
                .OrderBy(u => u.UserId)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // Логирование удаления пользователя
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _logService.LogAction(currentUserId, $"Удаление пользователя: ID={user.UserId}, Логин={user.Username}");

            return RedirectToPage();
        }
    }
}
