using AspNetBbs.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace AspNetBbs.Controllers;

public class AccountController(IConfiguration configuration, IWebHostEnvironment environment) : Controller
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
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var supportedProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Google", "Kakao", "Naver" };
        if (!supportedProviders.Contains(provider))
        {
            TempData["Message"] = "지원하지 않는 소셜 로그인 제공자입니다.";
            return RedirectToAction(nameof(Login));
        }

        var clientId = configuration[$"Authentication:{provider}:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId) || clientId.StartsWith("YOUR_"))
        {
            TempData["Message"] = $"{provider} 로그인을 사용하려면 appsettings.json에 ClientId 및 ClientSecret을 설정해야 합니다.";
            return RedirectToAction(nameof(Login));
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (!string.IsNullOrEmpty(remoteError))
        {
            TempData["Message"] = $"소셜 로그인 중 오류가 발생했습니다: {remoteError}";
            return RedirectToAction(nameof(Login));
        }

        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            TempData["Message"] = "소셜 계정 인증 정보를 가져오지 못했습니다.";
            return RedirectToAction(nameof(Login));
        }

        var principal = authenticateResult.Principal;
        var provider = authenticateResult.Properties?.Items[".AuthScheme"] ?? "Social";
        var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = principal.FindFirstValue(ClaimTypes.Name) ?? $"{provider} 사용자";
        var email = principal.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(providerKey))
        {
            TempData["Message"] = "소셜 계정의 고유 식별자(ID)를 확인할 수 없습니다.";
            return RedirectToAction(nameof(Login));
        }

        var success = await ProcessSocialLoginAsync(provider, providerKey, name, email);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!success)
        {
            TempData["Message"] = "소셜 로그인 계정 처리 중 오류가 발생했습니다.";
            return RedirectToAction(nameof(Login));
        }

        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Index", "Bbs");
    }

    [HttpGet]
    public async Task<IActionResult> MockSocialLogin(string provider, string? returnUrl = null)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var supported = new Dictionary<string, (string DefaultId, string DefaultName, string DefaultEmail)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Google"] = ("google_dev_1001", "구글테스터", "google_dev@gmail.com"),
            ["Kakao"] = ("kakao_dev_2002", "카카오테스터", "kakao_dev@kakao.com"),
            ["Naver"] = ("naver_dev_3003", "네이버테스터", "naver_dev@naver.com")
        };

        if (!supported.TryGetValue(provider, out var mockUser))
        {
            TempData["Message"] = "지원하지 않는 테스트 소셜 제공자입니다.";
            return RedirectToAction(nameof(Login));
        }

        var success = await ProcessSocialLoginAsync(provider, mockUser.DefaultId, mockUser.DefaultName, mockUser.DefaultEmail);
        if (!success)
        {
            TempData["Message"] = "테스트 소셜 로그인 처리 중 오류가 발생했습니다.";
            return RedirectToAction(nameof(Login));
        }

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
        await using var command = CreateCommand(connection, "ExecWeb.Member_Insert");
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

    private async Task<bool> ProcessSocialLoginAsync(string provider, string providerKey, string name, string? email)
    {
        try
        {
            var linkedMember = await FindSocialMemberAsync(provider, providerKey);
            if (linkedMember is not null)
            {
                HttpContext.Session.SetString(UserIdKey, linkedMember.Value.UserID);
                HttpContext.Session.SetString(UserNameKey, linkedMember.Value.UserName);
                return true;
            }

            var candidateUserId = $"{provider.ToLower()}_{providerKey}";
            if (candidateUserId.Length > 50)
                candidateUserId = candidateUserId[..50];

            var existingMember = await FindMemberAsync(candidateUserId);
            if (existingMember is null)
            {
                var randomPasswordHash = passwordHasher.HashPassword(candidateUserId, Guid.NewGuid().ToString("N"));
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                await using var insertCmd = CreateCommand(conn, "ExecWeb.Member_Insert");
                insertCmd.Parameters.Add("@UserID", SqlDbType.NVarChar, 50).Value = candidateUserId;
                insertCmd.Parameters.Add("@UserName", SqlDbType.NVarChar, 50).Value = name.Length > 50 ? name[..50] : name;
                insertCmd.Parameters.Add("@UserPWD", SqlDbType.NVarChar, 500).Value = randomPasswordHash;
                await insertCmd.ExecuteNonQueryAsync();
            }

            await using var linkConn = new SqlConnection(connectionString);
            await linkConn.OpenAsync();
            await using var linkCmd = CreateCommand(linkConn, "ExecWeb.MemberSocialLogin_Insert");
            linkCmd.Parameters.Add("@UserID", SqlDbType.NVarChar, 50).Value = candidateUserId;
            linkCmd.Parameters.Add("@Provider", SqlDbType.NVarChar, 50).Value = provider;
            linkCmd.Parameters.Add("@ProviderKey", SqlDbType.NVarChar, 100).Value = providerKey;
            linkCmd.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = (object?)email ?? DBNull.Value;
            await linkCmd.ExecuteNonQueryAsync();

            HttpContext.Session.SetString(UserIdKey, candidateUserId);
            HttpContext.Session.SetString(UserNameKey, name);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<(string UserID, string UserName, string? Email)?> FindSocialMemberAsync(string provider, string providerKey)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, "ExecWeb.MemberSocialLogin_SelectByProviderKey");
        command.Parameters.Add("@Provider", SqlDbType.NVarChar, 50).Value = provider;
        command.Parameters.Add("@ProviderKey", SqlDbType.NVarChar, 100).Value = providerKey;
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return (
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2)
            );
        }
        return null;
    }

    private async Task<(string UserID, string UserName, string Password)?> FindMemberAsync(string userId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, "ExecWeb.Member_SelectById");
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
