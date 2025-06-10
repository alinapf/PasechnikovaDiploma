using System;
using System.Collections.Generic;

namespace VeterinaryClinic.Models;

public partial class Service
{
    public int ServiceId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Specialization { get; set; } = null!;

    public int DurationMinutes { get; set; }

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
