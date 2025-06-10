using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeterinaryClinic.Models;

public partial class Client
{
    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("name")]
    [StringLength(100, ErrorMessage = "Не более 100 символов")]
    public string Name { get; set; } = string.Empty;

    [Column("phone")]
    [StringLength(20, ErrorMessage = "Не более 20 символов")]
    [RegularExpression(@"^\+?[0-9\s\-\(\)]+$", ErrorMessage = "Некорректный формат телефона")]
    public string Phone { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? PetName { get; set; }

    public string? PetType { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual User? User { get; set; }
}

public class ClientViewModel
{
    public int ClientId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
