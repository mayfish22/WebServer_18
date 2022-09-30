using Microsoft.AspNetCore.Mvc;
using WebServer.Services;

namespace WebServer.Components
{
    [ViewComponent(Name = "Language")]
    public class LanguageComponent : ViewComponent
    {
        private readonly SiteService _siteService;

        public LanguageComponent(SiteService siteService)
        {
            _siteService = siteService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            await Task.Yield();
            return View("Default", _siteService.GetCurrentCulture());
        }
    }
}