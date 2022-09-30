using WebServer.Extensions;
using WebServer.Models.WebServerDB;

namespace WebServer.Models
{
    public class SidebarViewModel
    {
        public Menu? CurrentPage { get; set; }
        public IEnumerable<HierarchyNode<Menu>> Menus { get; set; } = Enumerable.Empty<HierarchyNode<Menu>>();
    }
}