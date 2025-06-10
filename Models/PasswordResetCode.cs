using System.ComponentModel.DataAnnotations;

namespace VeterinaryClinic.Models
{
    public class PasswordResetCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Code { get; set; }

        [Required]
        public DateTime ExpirationDate { get; set; }

        public bool IsUsed { get; set; }
    }
}
