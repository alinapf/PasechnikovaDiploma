using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using System;

using VeterinaryClinic.Models;

namespace VeterinaryClinic.Pages
{
    public class AddDoctorsModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly IWebHostEnvironment _environment;

        public AddDoctorsModel(VeterinaryClinicContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public Doctor Doctor { get; set; }

        [BindProperty]
        public IFormFile PhotoFile { get; set; }
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            // ���� id �� ������ � ��������, ��������� ����� �� query-���������
            id ??= HttpContext.Request.Query["doctorId"].Count > 0
                ? int.Parse(HttpContext.Request.Query["doctorId"])
                : null;

            if (id.HasValue)
            {
                Doctor = await _context.Doctors.FindAsync(id);
                if (Doctor == null) return NotFound();
            }
            else
            {
                Doctor = new Doctor();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // ��������� �������� ����
            if (PhotoFile != null && PhotoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "doctors");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + PhotoFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await PhotoFile.CopyToAsync(fileStream);
                }

                Doctor.PhotoUrl = $"/uploads/doctors/{uniqueFileName}";
            }

            if (Doctor.DoctorId == 0)
            {
                // �������� ������ �����
                _context.Doctors.Add(Doctor);
            }
            else
            {
                // ���������� ������������� �����
                _context.Doctors.Update(Doctor);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Doctors");
        }
    }
}
