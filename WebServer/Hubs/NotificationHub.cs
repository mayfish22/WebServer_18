using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using WebServer.Models;
using WebServer.Models.WebServerDB;

namespace WebServer.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly WebServerDBContext _WebServerDBContext;

        public NotificationHub(IHttpContextAccessor httpContextAccessor,
            WebServerDBContext WebServerDBContext,
            ILogger<NotificationHub> logger)
        {
            _logger = logger;
            _WebServerDBContext = WebServerDBContext;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 連線
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnectedAsync()
        {
            try
            {
                var userSessionString = _httpContextAccessor.HttpContext.Session.GetString("CurrentUser");
                var currentUser = JsonSerializer.Deserialize<UserProfileModel>(userSessionString);

                if (currentUser == null)
                    return;

                // 取得使用者IP位置
                var ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress;

                // 記錄連線是哪位使用者
                var connection = await _WebServerDBContext.Connection.FindAsync(Context.ConnectionId);
                if (connection == null)
                {
                    await _WebServerDBContext.Connection.AddAsync(new Connection
                    {
                        ID = Context.ConnectionId,
                        UserID = currentUser.ID,
                        IP = ip?.ToString(),
                        ConnectDT = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    });
                }
                else
                {
                    connection.UserID = currentUser.ID;
                }
                await _WebServerDBContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "【{0}.{1}】", nameof(NotificationHub), nameof(OnConnectedAsync));
            }

        }

        /// <summary>
        /// 斷線
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public override Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                var userSessionString = _httpContextAccessor.HttpContext.Session.GetString("CurrentUser");
                var currentUser = JsonSerializer.Deserialize<UserProfileModel>(userSessionString);

                var connection = _WebServerDBContext.Connection.Find(Context.ConnectionId);
                if (connection != null)
                {
                    _WebServerDBContext.Connection.Remove(connection);
                    _WebServerDBContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "【{0}.{1}】", nameof(NotificationHub), nameof(OnDisconnectedAsync));
            }
            return base.OnDisconnectedAsync(exception);
        }
    }
}
