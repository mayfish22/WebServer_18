using Microsoft.AspNetCore.Mvc;
using WebServer.Services;
using WebServer.Models.WebServerDB;
using Microsoft.EntityFrameworkCore;
using WebServer.Models;

namespace WebServer.Components
{
    [ViewComponent(Name = "Sidebar")]
    public class SidebarComponent : ViewComponent
    {
        private readonly SiteService _siteService;
        private readonly WebServerDBContext _webServerDBContext;

        public SidebarComponent(SiteService siteService, WebServerDBContext webServerDBContext)
        {
            _siteService = siteService;
            _webServerDBContext = webServerDBContext;
        }

        public async Task<IViewComponentResult> InvokeAsync(string controller, string action)
        {
            //當前的頁面
            var currentPage = await (from n1 in _webServerDBContext.vwMenu
                                     join n2 in _webServerDBContext.Menu on n1.ID equals n2.ID
                                     where n2.Controller == controller && n2.Action == action
                                     select new Menu
                                     {
                                         ID = n1.ID,
                                         PID = n1.PID,
                                         IDs = n1.IDs,
                                         Code = n2.Code,
                                         Seq = n2.Seq,
                                         Icon = n2.Icon,
                                         Controller = n2.Controller,
                                         Action = n2.Action,
                                         IsEnabled = n2.IsEnabled,
                                     }).FirstOrDefaultAsync();
            //所有的選單
            var menus = await _siteService.GetMenu();
            return View(new SidebarViewModel
            {
                CurrentPage = currentPage,
                Menus = menus,
            });
        }
    }
}