using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace VeterinaryClinic.Pages
{
    public class ChangePasswordModel : PageModel
    {
        [BindProperty]
        public ChangePasswordViewModel PasswordModel { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // ������ ��������� ������
            // ...

            return RedirectToPage("/Profile");
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
