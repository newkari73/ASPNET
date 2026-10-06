using AspNetBbs.Models;
using AspNetBbs.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetBbs.Controllers;

public class PermissionController(IPermissionService permissionService) : Controller
{
    private string? CurrentUserId => HttpContext.Session.GetString("UserID");

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (CurrentUserId is null)
        {
            context.Result = RedirectToAction("Login", "Account", new { returnUrl = HttpContext.Request.Path });
            return;
        }

        var isAdmin = await permissionService.IsAdminAsync(CurrentUserId);
        if (!isAdmin)
        {
            TempData["ErrorMessage"] = "권한 관리 메뉴는 관리자만 접근할 수 있습니다.";
            context.Result = RedirectToAction("Index", "Home");
            return;
        }

        await next();
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? selectedUserId = null)
    {
        var members = await permissionService.GetAllMembersWithPermissionsAsync();

        if (string.IsNullOrWhiteSpace(selectedUserId) && members.Count > 0)
        {
            var defaultUser = members.FirstOrDefault(m => !m.IsAdmin) ?? members.First();
            selectedUserId = defaultUser.UserID;
        }

        var model = new PermissionManageViewModel
        {
            Members = members,
            SelectedUserId = selectedUserId
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(PermissionSaveRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserID))
        {
            TempData["ErrorMessage"] = "사용자 정보가 올바르지 않습니다.";
            return RedirectToAction(nameof(Index));
        }

        await permissionService.SavePermissionsAsync(request.UserID, request.Permissions);
        TempData["Message"] = $"[{request.UserID}] 사용자의 메뉴 권한 설정이 성공적으로 저장되었습니다.";

        return RedirectToAction(nameof(Index), new { selectedUserId = request.UserID });
    }
}