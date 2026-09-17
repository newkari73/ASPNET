using AspNetBbs.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetBbs.Controllers;

public class MonitoringController(ISystemMetricsService metricsService) : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (HttpContext.Session.GetString("UserID") is null)
        {
            context.Result = RedirectToAction("Login", "Account", new { returnUrl = HttpContext.Request.Path });
            return;
        }

        base.OnActionExecuting(context);
    }

    public IActionResult Index()
    {
        var model = metricsService.GetMetrics();
        return View(model);
    }

    [HttpGet]
    public IActionResult GetMetrics()
    {
        var model = metricsService.GetMetrics();
        return Json(model);
    }
}

