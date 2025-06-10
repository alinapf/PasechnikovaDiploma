using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using VeterinaryClinic.Models;

namespace VeterinaryClinic.Pages
{
    public class AddServiceModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;

        public AddServiceModel(VeterinaryClinicContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Service Service { get; set; } = new Service { ServiceId = 0 };

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                // Режим создания - оставляем пустую модель
                return Page();
            }

            // Ваш оригинальный подход с явной проекцией
            Service = await _context.Services
                .Where(s => s.ServiceId == id)
                .Select(s => new Service
                {
                    ServiceId = s.ServiceId, // Добавляем ID для редактирования
                    Name = s.Name,
                    Description = s.Description,
                    DurationMinutes = s.DurationMinutes,
                    Price = s.Price,
                    Specialization = s.Specialization
                })
                .AsNoTracking() 
                .FirstOrDefaultAsync();

            if (Service == null)
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

            // Разделяем логику создания и обновления
            if (Service.ServiceId == 0)
            {
                // Создание новой услуги
                var newService = new Service
                {
                    Name = Service.Name,
                    Description = Service.Description,
                    Specialization = Service.Specialization,
                    DurationMinutes = Service.DurationMinutes,
                    Price = Service.Price
                    // Другие необходимые поля
                };
                _context.Services.Add(newService);
            }
            else
            {
                // Альтернативный вариант с Attach
                var serviceToUpdate = new Service { ServiceId = Service.ServiceId };
                _context.Services.Attach(serviceToUpdate);

                // Обновляем только измененные поля
                serviceToUpdate.Name = Service.Name;
                serviceToUpdate.Description = Service.Description;
                serviceToUpdate.Specialization = Service.Specialization;
                serviceToUpdate.DurationMinutes = Service.DurationMinutes;
                serviceToUpdate.Price = Service.Price;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", $"Ошибка при сохранении: {(ex.InnerException?.Message ?? ex.Message)}");
                return Page();
            }

            return RedirectToPage("./Services");
        }
    }
}
