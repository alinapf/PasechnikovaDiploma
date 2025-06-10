using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using VeterinaryClinic.Models;

namespace VeterinaryClinic.Pages
{
    public class ServiceModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;

        public ServiceModel(VeterinaryClinicContext context)
        {
            _context = context;
        }

        public IList<Service> Services { get; set; } = new List<Service>();

        public async Task OnGetAsync()
        {
            Services = await _context.Services
                .Select(a => new Service
                {
                    ServiceId = a.ServiceId,
                    Name = a.Name,
                    Description = a.Description,
                    DurationMinutes = a.DurationMinutes,
                    Price = a.Price,
                    Specialization = a.Specialization
                }).ToListAsync();
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
