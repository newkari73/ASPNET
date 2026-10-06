using AspNetBbs.Models;
using AspNetBbs.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetBbs.Controllers;

public class MonitoringController(ISystemMetricsService metricsService, IPermissionService permissionService) : Controller
{
    private string CurrentUserId => HttpContext.Session.GetString("UserID")!;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (HttpContext.Session.GetString("UserID") is null)
        {
            context.Result = RedirectToAction("Login", "Account", new { returnUrl = HttpContext.Request.Path });
            return;
        }

        base.OnActionExecuting(context);
    }

    public async Task<IActionResult> Index()
    {
        if (!await permissionService.HasPermissionAsync(CurrentUserId, "MONITORING", PermissionType.Read))
        {
            TempData["ErrorMessage"] = "모니터링 조회 권한이 없습니다.";
            return RedirectToAction("Index", "Home");
        }

        var model = metricsService.GetMetrics();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetMetrics()
    {
        if (!await permissionService.HasPermissionAsync(CurrentUserId, "MONITORING", PermissionType.Read))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "모니터링 조회 권한이 없습니다." });
        }

        var model = metricsService.GetMetrics();
        return Json(model);
    }
}
