using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".AspNetBbs.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

builder.Services.AddSingleton<AspNetBbs.Services.ISystemMetricsService, AspNetBbs.Services.SystemMetricsService>();
builder.Services.AddScoped<AspNetBbs.Services.IPermissionService, AspNetBbs.Services.PermissionService>();

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = ".AspNetBbs.External";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
});

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !googleClientId.StartsWith("YOUR_"))
{
    authBuilder.AddGoogle(options =>
    {
        options.SignInScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret ?? string.Empty;
        options.CallbackPath = "/signin-google";
    });
}

var kakaoClientId = builder.Configuration["Authentication:Kakao:ClientId"];
var kakaoClientSecret = builder.Configuration["Authentication:Kakao:ClientSecret"];
if (!string.IsNullOrWhiteSpace(kakaoClientId) && !kakaoClientId.StartsWith("YOUR_"))
{
    authBuilder.AddOAuth("Kakao", options =>
    {
        options.SignInScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
        options.ClientId = kakaoClientId;
        options.ClientSecret = kakaoClientSecret ?? string.Empty;
        options.CallbackPath = "/signin-kakao";
        options.AuthorizationEndpoint = "https://kauth.kakao.com/oauth/authorize";
        options.TokenEndpoint = "https://kauth.kakao.com/oauth/token";
        options.UserInformationEndpoint = "https://kapi.kakao.com/v2/user/me";
        options.ClaimActions.MapJsonKey(System.Security.Claims.ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonSubKey(System.Security.Claims.ClaimTypes.Name, "properties", "nickname");
        options.ClaimActions.MapJsonSubKey(System.Security.Claims.ClaimTypes.Email, "kakao_account", "email");
        options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
                var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();
                using var user = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                context.RunClaimActions(user.RootElement);
            }
        };
    });
}

var naverClientId = builder.Configuration["Authentication:Naver:ClientId"];
var naverClientSecret = builder.Configuration["Authentication:Naver:ClientSecret"];
if (!string.IsNullOrWhiteSpace(naverClientId) && !naverClientId.StartsWith("YOUR_"))
{
    authBuilder.AddOAuth("Naver", options =>
    {
        options.SignInScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
        options.ClientId = naverClientId;
        options.ClientSecret = naverClientSecret ?? string.Empty;
        options.CallbackPath = "/signin-naver";
        options.AuthorizationEndpoint = "https://nid.naver.com/oauth2.0/authorize";
        options.TokenEndpoint = "https://nid.naver.com/oauth2.0/token";
        options.UserInformationEndpoint = "https://openapi.naver.com/v1/nid/me";
        options.ClaimActions.MapJsonSubKey(System.Security.Claims.ClaimTypes.NameIdentifier, "response", "id");
        options.ClaimActions.MapJsonSubKey(System.Security.Claims.ClaimTypes.Name, "response", "name");
        options.ClaimActions.MapJsonSubKey(System.Security.Claims.ClaimTypes.Email, "response", "email");
        options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
                var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();
                using var user = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                context.RunClaimActions(user.RootElement);
            }
        };
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();
app.UseSession();
app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
