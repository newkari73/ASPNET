using System.ComponentModel.DataAnnotations;

namespace AspNetBbs.Models;

public class ScheduleItem
{
    public int ID { get; set; }
    public string UserID { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Contents { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public class ScheduleFormModel
{
    [Required(ErrorMessage = "시작 날짜를 입력해 주세요.")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "시작 시간을 입력해 주세요.")]
    public TimeSpan StartTime { get; set; }

    [Required(ErrorMessage = "완료 날짜를 입력해 주세요.")]
    public DateTime EndDate { get; set; }

    [Required(ErrorMessage = "완료 시간을 입력해 주세요.")]
    public TimeSpan EndTime { get; set; }

    [Required(ErrorMessage = "일정 제목을 입력해 주세요.")]
    [StringLength(200, ErrorMessage = "제목은 200자 이내로 입력해 주세요.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "내용은 2,000자 이내로 입력해 주세요.")]
    public string Contents { get; set; } = string.Empty;

    public DateTime StartDateTime => StartDate.Date.Add(StartTime);
    public DateTime EndDateTime => EndDate.Date.Add(EndTime);
}

public class ScheduleViewModel
{
    public DateTime Month { get; init; }
    public DateTime SelectedDate { get; init; }
    public IReadOnlyList<ScheduleItem> Schedules { get; init; } = [];
    public IReadOnlyList<ScheduleItem> MonthSchedules { get; init; } = [];
    public IReadOnlySet<DateTime> DatesWithSchedules { get; init; } = new HashSet<DateTime>();
}