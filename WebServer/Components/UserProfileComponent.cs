using Microsoft.AspNetCore.Mvc;
using WebServer.Services;

namespace WebServer.Components
{
    [ViewComponent(Name = "UserProfile")]
    public class UserProfileComponent : ViewComponent
    {
        private readonly SiteService _siteService;

        public UserProfileComponent(SiteService siteService)
        {
            _siteService = siteService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            return View("Default", await _siteService.GetUserProfile());
        }
    }
}