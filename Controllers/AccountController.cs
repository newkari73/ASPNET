using AspNetBbs.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AspNetBbs.Controllers;

public class AccountController(IConfiguration configuration) : Controller
{
    private const string UserIdKey = "UserID";
    private const string UserNameKey = "UserName";
    private readonly string connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection이 설정되지 않았습니다.");
    private readonly PasswordHasher<string> passwordHasher = new();

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (UserID is not null)
        {
            return RedirectToAction("Index", "Bbs");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var member = await FindMemberAsync(model.UserID);
        if (member is null || passwordHasher.VerifyHashedPassword(model.UserID, member.Value.Password, model.UserPWD) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "아이디 또는 비밀번호가 올바르지 않습니다.");
            return View(model);
        }

        HttpContext.Session.SetString(UserIdKey, member.Value.UserID);
        HttpContext.Session.SetString(UserNameKey, member.Value.UserName);
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Index", "Bbs");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = model.UserID.Trim();
        var passwordHash = passwordHasher.HashPassword(userId, model.UserPWD);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, "dbo.Member_Insert");
        command.Parameters.Add("@UserID", SqlDbType.NVarChar, 50).Value = userId;
        command.Parameters.Add("@UserName", SqlDbType.NVarChar, 50).Value = model.UserName.Trim();
        command.Parameters.Add("@UserPWD", SqlDbType.NVarChar, 500).Value = passwordHash;
        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            ModelState.AddModelError(nameof(model.UserID), "이미 사용 중인 아이디입니다.");
            return View(model);
        }

        TempData["Message"] = "회원가입이 완료되었습니다. 로그인해 주세요.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    private string? UserID => HttpContext.Session.GetString(UserIdKey);

    private async Task<(string UserID, string UserName, string Password)?> FindMemberAsync(string userId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, "dbo.Member_SelectById");
        command.Parameters.Add("@UserID", SqlDbType.NVarChar, 50).Value = userId.Trim();
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync()
            ? (reader.GetString(0), reader.GetString(1), reader.GetString(2))
            : null;
    }

    private static SqlCommand CreateCommand(SqlConnection connection, string procedureName) => new(procedureName, connection)
    {
        CommandType = CommandType.StoredProcedure
    };
}