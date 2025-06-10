using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeterinaryClinic.Models;

public partial class Review
{
    [Column("review_id")]
    public int ReviewId { get; set; }

    [Column("doctor_id")]
    [Required]
    public int DoctorId { get; set; }

    [Column("client_id")]
    [Required]
    public int ClientId { get; set; }

    public int? AppointmentId { get; set; }

    [Column("rating")]
    [Range(1, 5)] // Допустимые значения от 1 до 5
    public int Rating { get; set; }

    [Column("comment")]
    [Required]
    [StringLength(500)] // Максимальная длина комментария
    public string Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Appointment Appointment { get; set; }

    public virtual Client Client { get; set; }

    public virtual Doctor Doctor { get; set; }
    [Column("status")]
    public string Status { get; set; } = "На модерации";
}
public class ReviewViewModel
{
    public int ReviewId { get; set; } // Добавляем ID для управления
    public string ClientName { get; set; }
    public string DoctorName { get; set; }
    public string Comment { get; set; }
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } // Добавляем статус
    public bool IsEditable { get; set; } // Флаг для режима редактирования
}