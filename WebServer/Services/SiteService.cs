using System.Text;
using System.Text.Json;
using WebServer.Models.WebServerDB;
using System.Security.Cryptography;
using WebServer.Models;
using Microsoft.AspNetCore.Localization;
using WebServer.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Microsoft.Extensions.Localization;
using WebServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Xml.Linq;

namespace WebServer.Services
{
    public class SiteService
    {
        private readonly WebServerDBContext _WebServerDBContext;
        private readonly IHttpContextAccessor _context;
        private readonly IConfiguration _configuration;
        private readonly IStringLocalizer<Resource> _localizer;
        private readonly IHubContext<NotificationHub> _hubContext;

        public SiteService(WebServerDBContext WebServerDBContext,
            IHttpContextAccessor context,
            IConfiguration configuration,
            IStringLocalizer<Resource> localizer,
            IHubContext<NotificationHub> hubContext)
        {
            _WebServerDBContext = WebServerDBContext;
            _context = context;
            _configuration = configuration;
            _localizer = localizer;
            _hubContext = hubContext;
        }

        /// <summary>
        /// 字串加密
        /// </summary>
        /// <param name="input"></param>
        /// <returns>SHA512</returns>
        public string EncoderSHA512(string input)
        {
            string salt = _configuration.GetValue<string>("Salt");
            var message = Encoding.UTF8.GetBytes(salt + input);
            using (var alg = SHA512.Create())
            {
                string output = string.Empty;

                var hashValue = alg.ComputeHash(message);
                foreach (byte x in hashValue)
                {
                    output += String.Format("{0:x2}", x);
                }
                return output;
            }
        }

        /// <summary>
        /// 記錄使用者資訊到Session
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task SetUserProfile(string id)
        {
            await Task.Yield();
            var user = _WebServerDBContext.User.Find(id);
            _context.HttpContext.Session.SetString("CurrentUser", JsonSerializer.Serialize(new UserProfileModel
            {
                ID = user.ID,
                Account = user.Account,
                DisplayName = user.Name,
                Email = user.Email,
            }));
        }
        /// <summary>
        /// 從Session讀取使用者資訊
        /// </summary>
        /// <returns>UserProfile</returns>
        public async Task<UserProfileModel> GetUserProfile()
        {
            await Task.Yield();
            var UserSessionString = _context.HttpContext.Session.GetString("CurrentUser");
            if (string.IsNullOrEmpty(UserSessionString))
            {
                return null;
            }
            return JsonSerializer.Deserialize<UserProfileModel>(UserSessionString);
        }

        /// <summary>
        /// 取得語言設定
        /// </summary>
        /// <returns></returns>
        public string[] GetCultures()
        {
            return _WebServerDBContext.Language
                    .Where(s => s.IsEnabled == 1)
                    .OrderBy(s => s.Seq)
                    .Select(s => s.ID)
                    .ToArray();
        }

        /// <summary>
        /// 當前的語言設定
        /// </summary>
        /// <returns></returns>
        public string GetCurrentCulture()
        {
            var cultures = GetCultures();
            var currentCulture = cultures[0];
            if (_context.HttpContext.Request.Cookies.ContainsKey(CookieRequestCultureProvider.DefaultCookieName))
            {
                //若有記錄
                currentCulture = CookieRequestCultureProvider.ParseCookieValue(_context.HttpContext.Request.Cookies[CookieRequestCultureProvider.DefaultCookieName]).Cultures.FirstOrDefault().Value;
            }
            if (Array.IndexOf(cultures, currentCulture) < 0)
            {
                //沒有記錄
                currentCulture = cultures[0];
            }
            return currentCulture;
        }

        /// <summary>
        /// 設定語言
        /// </summary>
        /// <param name="culture"></param>
        public void SetCulture(string? culture = null)
        {
            if (string.IsNullOrEmpty(culture))
            {
                if (_context.HttpContext.Request.Cookies.ContainsKey(CookieRequestCultureProvider.DefaultCookieName))
                {
                    culture = CookieRequestCultureProvider.ParseCookieValue(_context.HttpContext.Request.Cookies[CookieRequestCultureProvider.DefaultCookieName]).Cultures.FirstOrDefault().Value;
                }
                else
                {
                    culture = GetCultures()[0];
                }
            }
            //將設定寫入Cookie
            _context.HttpContext.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );
        }

        /// <summary>
        /// for SidebarComponent
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Extensions.HierarchyNode<Menu>>> GetMenu()
        {
            /*
             * ToListAsync() 需要
             * using Microsoft.EntityFrameworkCore;
             */
            var menus = await (from n1 in _WebServerDBContext.vwMenu
                               join n2 in _WebServerDBContext.Menu on n1.ID equals n2.ID
                               join n3 in _WebServerDBContext.MenuTranslation on new { MenuID = n2.ID, LanguageID = GetCurrentCulture() } equals new { n3.MenuID, n3.LanguageID } into tempN3
                               from n3 in tempN3.DefaultIfEmpty()
                               select new Menu
                               {
                                   ID = n1.ID,
                                   PID = n1.PID,
                                   IDs = n1.IDs,
                                   GID = Guid.Parse(n1.ID),
                                   GPID = string.IsNullOrEmpty(n1.PID) ? null : Guid.Parse(n1.PID),
                                   Code = n2.Code,
                                   Name = n3 == null ? string.Empty : n3.Name,
                                   Description = n3 == null ? string.Empty : n3.Description,
                                   Seq = n2.Seq,
                                   Icon = n2.Icon,
                                   Controller = n2.Controller,
                                   Action = n2.Action,
                                   IsEnabled = n2.IsEnabled,
                               }).ToListAsync();
            return menus.AsHierarchy(s => s.GID, s => s.GPID);
        }

        #region Datatable
        /// <summary>
        /// 取得欄位屬性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnList"></param>
        /// <returns></returns>
        public async Task<List<DatatableColumn>> GetDatatableColumns<T>(List<string> columnList) where T : class
        {
            await Task.Yield();
            Type modelType = typeof(T);
            List<DatatableColumn> aColumn = new();

            // ModelMetadataTye屬性
            var attributeMetadataType = (ModelMetadataTypeAttribute)modelType.GetCustomAttributes(typeof(ModelMetadataTypeAttribute), true).FirstOrDefault();
            // Model屬性
            var modeldataType = modelType.GetProperties();

            aColumn = columnList.Select((s, i) => new DatatableColumn
            {
                Seq = (long)i,
                Name = s,
                DisplayName = ColumnDisplayName(modeldataType, attributeMetadataType, s),
                DisplayType = nameof(DisplayTypeEnum.Text),
                SortingType = nameof(SortingTypeEnum.Enabled),
                IsVisible = true,
            }).ToList();

            foreach (var item in aColumn)
            {
                var displayType = (DisplayTypeAttribute)modelType.GetProperty(item.Name)?.GetCustomAttribute(typeof(DisplayTypeAttribute));
                if (displayType != null)
                {
                    item.DisplayType = Enum.GetName(typeof(DisplayTypeEnum), displayType.Type);
                    item.DisplayParameters = displayType.Parameters;
                }
                var sortingType = (SortingTypeAttribute)modelType.GetProperty(item.Name)?.GetCustomAttribute(typeof(SortingTypeAttribute));
                if (sortingType != null)
                {
                    item.SortingType = Enum.GetName(typeof(SortingTypeEnum), sortingType.Type);
                    item.SortingParameters = sortingType.Parameters;
                }
            }

            return aColumn;
        }

        /// <summary>
        /// 取得欄位名稱
        /// </summary>
        /// <param name="modeldataType"></param>
        /// <param name="attributeMetadataType"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        private string ColumnDisplayName(PropertyInfo[] modeldataType, ModelMetadataTypeAttribute attributeMetadataType, string columnName)
        {
            if (modeldataType != null && attributeMetadataType == null)
            {
                var datas = (modeldataType.FirstOrDefault(f => f.Name == columnName).GetCustomAttributes(typeof(DisplayAttribute)).FirstOrDefault());
                DisplayAttribute tmp = (DisplayAttribute)datas;
                if (tmp != null)
                {
                    return _localizer[tmp.Name];
                }
                else
                {
                    datas = (modeldataType.FirstOrDefault(f => f.Name == columnName).GetCustomAttributes(typeof(DisplayNameAttribute)).FirstOrDefault());
                    var tmp2 = (DisplayNameAttribute)datas;
                    return tmp2 != null ? _localizer[tmp2.DisplayName] : columnName;
                }
            }
            var metaProperties = attributeMetadataType.MetadataType.GetProperties();
            if (metaProperties != null)
            {
                var property = metaProperties.Where(a => a.Name == columnName).FirstOrDefault();
                if (property != null)
                {
                    var datas = property.GetCustomAttributes(typeof(DisplayAttribute)).FirstOrDefault();
                    DisplayAttribute tmp = (DisplayAttribute)datas;
                    if (tmp != null)
                    {
                        return _localizer[tmp.Name];
                    }
                    else
                    {
                        datas = property.GetCustomAttributes(typeof(DisplayNameAttribute)).FirstOrDefault();
                        DisplayNameAttribute tmp2 = (DisplayNameAttribute)datas;
                        return tmp2 != null ? _localizer[tmp2.DisplayName] : columnName;
                    }
                }
            }
            return columnName;
        }
        #endregion

        /// <summary>
        /// 清除 Connection 記錄
        /// </summary>
        /// <returns></returns>
        public async Task Init()
        {
            var connections = _WebServerDBContext.Connection.Select(s => s);
            _WebServerDBContext.RemoveRange(connections);
            await _WebServerDBContext.SaveChangesAsync();
        }

        /// <summary>
        /// 傳送訊息給指定使用者
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task Send(string userId, string title, string message)
        {
            try
            {
                var data = new
                {
                    title = title,
                    message = message,
                };
                var connections = await _WebServerDBContext.Connection.Where(s => s.UserID.Equals(userId)).Select(s => s.ID).ToListAsync();
                await _hubContext.Clients.Clients(connections).SendAsync("ReceiveMessage", System.Text.Json.JsonSerializer.Serialize(data));
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
