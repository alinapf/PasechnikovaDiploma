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

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        public IList<Log> Logs { get; set; }

        public async Task OnGetAsync()
        {
            var logs = _context.Logs.Include(l => l.User).AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                logs = logs.Where(l =>
                    l.User.Username.Contains(SearchString) ||
                    (l.User == null && "System".Contains(SearchString)));
            }

            if (SelectedDate.HasValue)
            {
                logs = logs.Where(l => l.Timestamp.HasValue &&
                                      l.Timestamp.Value.Date == SelectedDate.Value.Date);
            }

            Logs = await logs
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}
