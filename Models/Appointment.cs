using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeterinaryClinic.Models;

public partial class Appointment
{
    [Column("appointment_id")]
    public int AppointmentId { get; set; }

    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("doctor_id")]
    public int DoctorId { get; set; }

    public int? ServiceId { get; set; }

    [Column("status")]
    public string Status { get; set; }

    [Column("date")]
    public DateTime Date { get; set; }

    [Column("time")]
    public TimeSpan Time { get; set; }

    public string? Notes { get; set; }

    public string? Diagnosis { get; set; }

    public string? Prescription { get; set; }

    public virtual Client? Client { get; set; }

    public virtual Doctor? Doctor { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Service? Service { get; set; }


}

public class AppointmentViewModel
{
    public int AppointmentId { get; set; }
    public string ClientName { get; set; }
    public string DoctorName { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public string Status { get; set; }
    public string ServiceName { get; set; }
    public int DurationMinutes { get; set; }
}
