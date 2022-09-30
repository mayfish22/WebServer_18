using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebServer.Models.WebServerDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using WebServer.Hubs;

namespace WebServer.Controllers
{
    [Route("api")]
    //使用JwtBearer
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class WebApiController : Controller
    {
        private readonly WebServerDBContext _WebServerDBContext;
        private readonly IHttpContextAccessor _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public WebApiController(WebServerDBContext WebServerDBContext
            , IHttpContextAccessor context
            , IHubContext<NotificationHub> hubContext)
        {
            _WebServerDBContext = WebServerDBContext;
            _context = context;
            _hubContext = hubContext;
        }
        // api/Test
        [HttpPost("Test")]
        public async Task<IActionResult> Test()
        {
            var userID = User?.Identity?.Name;
            var user = await _WebServerDBContext.User.FindAsync(userID);
            return Json(new
            {
                email = user?.Email,
            });
        }

        // https://localhost:7120/api/PunchIn/{cardNo}
        [HttpPost("PunchIn/{cardNo}")]
        public async Task<IActionResult> PunchIn(string cardNo)
        {
            //先檢查卡號是否存在
            var card = await _WebServerDBContext.Card.Where(s => s.CardNo == cardNo).FirstOrDefaultAsync();
            if (card == null)
            {
                //卡片不存在, 便新增卡片
                card = new Card
                {
                    ID = Guid.NewGuid().ToString(),
                    CardNo = cardNo,
                };
                await _WebServerDBContext.Card.AddAsync(card);
            }
            //新增打卡記錄
            var cardHistory = new CardHistory
            {
                ID = Guid.NewGuid().ToString(),
                CardID = card.ID,
                PunchInDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            };
            await _WebServerDBContext.CardHistory.AddAsync(cardHistory);
            //儲存變更
            await _WebServerDBContext.SaveChangesAsync();
            //送出更新畫面的訊息
            await _hubContext.Clients.All.SendAsync("RefreshDatatable", "timeSheetDatatable");
            return Json(new
            {
                PunchInDateTime = cardHistory.PunchInDateTime,
            });
        }
    }
}