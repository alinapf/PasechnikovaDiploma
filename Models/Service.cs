using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VeterinaryClinic.Models;

public partial class Service
{
    public int ServiceId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Specialization { get; set; } = null!;

    [Required]
    [Range(1, 480, ErrorMessage = "Длительность должна быть от 1 до 480 минут")]
    public int DurationMinutes { get; set; } = 30; 

    public decimal? Price { get; set; }
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}

public class ServiceViewModel
{
    public int ServiceId { get; set; }
    public string Name { get; set; }

    public string? Description { get; set; }

    public string Specialization { get; set; } = null!;

    public int DurationMinutes { get; set; }

    public decimal? Price { get; set; }
}
