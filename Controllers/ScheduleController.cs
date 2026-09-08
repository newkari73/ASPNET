using AspNetBbs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AspNetBbs.Controllers;

public class ScheduleController(IConfiguration configuration) : Controller
{
    private readonly string connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection이 설정되지 않았습니다.");

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (HttpContext.Session.GetString("UserID") is null)
        {
            context.Result = RedirectToAction("Login", "Account", new { returnUrl = HttpContext.Request.Path });
            return;
        }

        base.OnActionExecuting(context);
    }

    public async Task<IActionResult> Index(DateTime? month, DateTime? selectedDate)
    {
        var currentMonth = new DateTime((month ?? DateTime.Today).Year, (month ?? DateTime.Today).Month, 1);
        var selected = (selectedDate ?? currentMonth).Date;
        var userId = CurrentUserId;
        var schedules = await FindSchedulesAsync(userId, currentMonth);

        return View(new ScheduleViewModel
        {
            Month = currentMonth,
            SelectedDate = selected,
            Schedules = schedules.Where(item => item.StartDateTime.Date == selected).ToList(),
            MonthSchedules = schedules,
            DatesWithSchedules = schedules
                .SelectMany(item => Enumerable.Range(0, (item.EndDateTime.Date - item.StartDateTime.Date).Days + 1)
                    .Select(offset => item.StartDateTime.Date.AddDays(offset)))
                .ToHashSet()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ScheduleFormModel model)
    {
        if (!ModelState.IsValid || model.StartDateTime.Date < DateTime.Today || model.EndDateTime < model.StartDateTime)
        {
            return BadRequest(new { success = false, message = "지난 날짜는 등록할 수 없으며 완료 일시는 시작 일시보다 빠를 수 없습니다." });
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = CreateCommand(connection, "dbo.Schedule_Insert");
            command.Parameters.Add("@UserID", SqlDbType.NVarChar, 50).Value = CurrentUserId;
            command.Parameters.Add("@StartDateTime", SqlDbType.DateTime2).Value = model.StartDateTime;
            command.Parameters.Add("@EndDateTime", SqlDbType.DateTime2).Value = model.EndDateTime;
            command.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = model.Title;
            command.Parameters.Add("@Contents", SqlDbType.NVarChar, 2000).Value = (object?)model.Contents ?? DBNull.Value;
            await command.ExecuteNonQueryAsync();
            return Json(new { success = true, selectedDate = model.StartDateTime.ToString("yyyy-MM-dd") });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "일정 등록 중 오류가 발생했습니다." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, DateTime selectedDate)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, "dbo.Schedule_Delete");
        command.Parameters.Add("@ID", SqlDbType.Int).Value = id;
        command.Parameters.Add("@UserID", SqlDbType.NVarChar, 50).Value = CurrentUserId;
        await command.ExecuteNonQueryAsync();
        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            return Json(new { success = true });
        }
        return RedirectToAction(nameof(Index), new { selectedDate = selectedDate.ToString("yyyy-MM-dd") });
    }

    private async Task<List<ScheduleItem>> FindSchedulesAsync(string userId, DateTime month)
    {
        var schedules = new List<ScheduleItem>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, "dbo.Schedule_SelectByMonth");
        command.Parameters.Add("@UserID", SqlDbType.NVarChar, 50).Value = userId;
        command.Parameters.Add("@Month", SqlDbType.Date).Value = month.Date;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            schedules.Add(new ScheduleItem
            {
                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                UserID = reader.GetString(reader.GetOrdinal("UserID")),
                StartDateTime = reader.GetDateTime(reader.GetOrdinal("StartDateTime")),
                EndDateTime = reader.GetDateTime(reader.GetOrdinal("EndDateTime")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Contents = reader.IsDBNull(reader.GetOrdinal("Contents")) ? string.Empty : reader.GetString(reader.GetOrdinal("Contents")),
                RegDate = reader.GetDateTime(reader.GetOrdinal("RegDate"))
            });
        }

        return schedules;
    }

    private string CurrentUserId => HttpContext.Session.GetString("UserID")!;

    private static SqlCommand CreateCommand(SqlConnection connection, string procedureName) => new(procedureName, connection)
    {
        CommandType = CommandType.StoredProcedure
    };
}