using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Models;

namespace VeterinaryClinic.Pages
{
    public class LogsModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;

        public LogsModel(VeterinaryClinicContext context)
        {
            _context = context;
        }

        public IList<Log> Logs { get; set; }

        public async Task OnGetAsync()
        {
            Logs = await _context.Logs
                .Include(l => l.User) // если нужно загрузить связанные данные пользователя
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}
