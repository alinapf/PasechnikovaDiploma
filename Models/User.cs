using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeterinaryClinic.Models;

public partial class User
{
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("username")]
    [Required(ErrorMessage = "Имя пользователя обязательно.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Имя пользователя должно быть от 3 до 50 символов.")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Логин может содержать только буквы, цифры и подчеркивание.")]
    public string Username { get; set; }

    [Column("email")]
    [Required(ErrorMessage = "Электронная почта обязательна.")]
    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
    [StringLength(100, ErrorMessage = "Электронная почта не может превышать 100 символов.")]
    public string Email { get; set; }

    [Column("password")]
    [Required(ErrorMessage = "Пароль обязателен.")]
    [StringLength(255, MinimumLength = 8, ErrorMessage = "Пароль должен содержать минимум 8 символов.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Пароль должен содержать заглавные и строчные буквы, цифры и спецсимволы.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [NotMapped] // Это поле не будет сохраняться в БД
    [Compare("Password", ErrorMessage = "Пароли не совпадают.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; }

    [Column("role")]
    [StringLength(20)]
    public string Role { get; set; } = "Client"; // Значение по умолчанию

    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();

    public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();

    public virtual ICollection<Log> Logs { get; set; } = new List<Log>();

}
public class UserProfileViewModel
{
    public int UserId { get; set; }

    [Display(Name = "Имя пользователя")]
    public string Username { get; set; }

    [Display(Name = "Email")]
    [EmailAddress]
    public string Email { get; set; }

    // Клиентские поля
    [Display(Name = "ФИО")]
    [StringLength(100, ErrorMessage = "Не более 100 символов")]
    public string ClientName { get; set; }

    [Display(Name = "Телефон")]
    [StringLength(20, ErrorMessage = "Не более 20 символов")]
    [RegularExpression(@"^\+?[0-9\s\-\(\)]+$", ErrorMessage = "Некорректный формат телефона")]
    public string Phone { get; set; }

    [Display(Name = "Адрес")]
    public string Address { get; set; }

    [Display(Name = "Кличка питомца")]
    public string PetName { get; set; }

    [Display(Name = "Вид питомца")]
    public string PetType { get; set; }
}
