using VeterinaryClinic.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeterinaryClinic.Models;

public class Doctor
{
    [Column("doctor_id")]
    public int DoctorId { get; set; }
    [Column("user_id")]
    public int UserId { get; set; }
    [Column("name")]
    public string Name { get; set; }
    [Column("specialization")]
    public string Specialization { get; set; }
    [Column("rating")]
    public int Rating { get; set; }
    [Column("bio")]
    public string Bio { get; set; }
    [Column("photo_url")]
    public string PhotoUrl { get; set; }

    public User User { get; set; }
    public List<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}



// ViewModel для отображения
public class DoctorViewModel
{
    public int DoctorId { get; set; }
    public string Name { get; set; }
    public string Specialization { get; set; }
    public string Schedule { get; set; }
    public int Rating { get; set; }
    public string PhotoUrl { get; set; }
    public string Bio { get; set; }
    public List<ReviewViewModel> Reviews { get; set; } = new List<ReviewViewModel>();
    public List<string> AvailableTimes { get; set; }
    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}


