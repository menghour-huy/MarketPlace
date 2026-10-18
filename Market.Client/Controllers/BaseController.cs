using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Market.Client.Controllers
{
    /// <summary>
    /// Base controller for all application controllers.
    /// Automatically populates ViewBag with user session data from HttpContext.Items
    /// </summary>
    public class BaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Populate ViewBag with user data from HttpContext.Items
            if (context.HttpContext.Items.TryGetValue("UserImageUrl", out var userImageUrl))
            {
                ViewBag.UserImageUrl = userImageUrl;
            }

            if (context.HttpContext.Items.TryGetValue("UserFullName", out var userFullName))
            {
                ViewBag.UserFullName = userFullName;
            }
        }
    }
}
