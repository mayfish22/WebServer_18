using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebServer.Services;

namespace WebServer.Filters
{
    public class AuthorizeFilter : IActionFilter
    {
        private readonly SiteService _siteService;
        public AuthorizeFilter(SiteService siteService)
        {
            _siteService = siteService;
        }

        void IActionFilter.OnActionExecuted(ActionExecutedContext context)
        {
            return;
        }

        void IActionFilter.OnActionExecuting(ActionExecutingContext context)
        {
            string returnUrl = context.HttpContext.Request.Path.ToString();
            string fromSring = !string.IsNullOrEmpty(context.HttpContext.Request.Query["From"].ToString()) ? "?From=" + context.HttpContext.Request.Query["From"].ToString() : "";
            returnUrl += fromSring;

            var userInfo = _siteService.GetUserProfile().Result;
            if (userInfo == null)
            {
                //導頁至登入頁
                context.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new
                    {
                        action = "Signin",
                        controller = "Account",
                        area = "",
                        returnUrl = returnUrl
                    }));
            }
        }
    }
}