using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace VeterinaryClinic.Models;

public class DoctorSchedule
{
    public int ScheduleId { get; set; }
    public int DoctorId { get; set; }
    public DateTime WorkDate { get; set; }
    public string WorkHours { get; set; }
}
public class ScheduleEvent
{
    public string Title { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Color { get; set; }
    public bool IsBreak { get; set; }
}




public class DoctorScheduleViewModel
{
    public int DoctorId { get; set; }
    public string DoctorName { get; set; }
    public List<ScheduleEvent> Events { get; set; } = new List<ScheduleEvent>();
    public List<DaySchedule> Schedules { get; set; } = new List<DaySchedule>();
}

public class DaySchedule
{
    public byte DayOfWeek { get; set; }
    public string DayName { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsWorkingDay { get; set; }
}
