using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebServer.Extensions;
using WebServer.Models.WebServerDB;
using WebServer.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using WebServer.Models;
using System.Linq;

namespace WebServer.Controllers
{
    [Authorize]
    [ServiceFilter(typeof(WebServer.Filters.AuthorizeFilter))]
    public class CommonController : Controller
    {
        private readonly WebServerDBContext _WebServerDBContext;
        private readonly SiteService _SiteService;

        public CommonController(WebServerDBContext WebServerDBContext
            , SiteService SiteService)
        {
            _WebServerDBContext = WebServerDBContext;
            _SiteService = SiteService;
        }

        public class Select2ProcessResults
        {
            [JsonPropertyName("results")]
            public object? Results { get; set; }
            [JsonPropertyName("pagination")]
            public bool Pagination { get; set; }
            [JsonPropertyName("error")]
            public string? ErrorMessage { get; set; }
        }
        public class Select2Result
        {
            [JsonPropertyName("id")]
            public string? ID { get; set; }
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        #region TestRemoteData
        public class TestRemoteDataPara
        {
            //關鍵字查詢
            public string? Parameter { get; set; }
            //分頁頁碼
            public int Page { get; set; }
            //顯示筆數
            public int Rows { get; set; }
        }
        public class TestRemoteDataResult
        {
            [JsonPropertyName("id")]
            public string? ID { get; set; }
            [JsonPropertyName("account")]
            public string? Account { get; set; }
            [JsonPropertyName("name")]
            public string? Name { get; set; }
            [JsonPropertyName("email")]
            public string? Email { get; set; }
        }
        [HttpPost]
        public async Task<IActionResult> TestRemoteData([FromBody]TestRemoteDataPara info)
        {
            try
            {
                //後續資料比對都用小寫
                info.Parameter = (info.Parameter ?? "").ToLower();

                var results = from a in _WebServerDBContext.User
                              where a.Account.ToLower().Contains(info.Parameter)
                                || a.Name.ToLower().Contains(info.Parameter)
                                || a.Email.ToLower().Contains(info.Parameter)
                              orderby a.Account
                              select new TestRemoteDataResult
                              {
                                  ID = a.ID,
                                  Account = a.Account,
                                  Name = a.Name,
                                  Email = a.Email,
                              };
                //總筆數
                var nTotalCount = await results.CountAsync();
                //要顯示的起始筆數
                var start = (info.Page - 1) * info.Rows;
                //顯示的筆數
                var r = await results.Skip(start).Take(info.Rows).ToListAsync();
                //是否還有資料
                var p = (nTotalCount - start) > info.Rows;

                return new SystemTextJsonResult(new Select2ProcessResults
                {
                    Results = r.Select(s => new Select2Result
                    {
                        ID = s.ID,
                        Text = s.Name,
                    }),
                    Pagination = p
                });
            }
            catch (Exception e)
            {
                return new SystemTextJsonResult(new Select2ProcessResults
                {
                    Results = new List<Select2Result>(),
                    Pagination = false,
                    ErrorMessage = e.Message,
                });
            }
        }
        #endregion

        #region FetchUser
        public class FetchUserPara
        {
            //初始值
            public List<string>? Values { get; set; }
            //關鍵字查詢
            public string? Parameter { get; set; }
            //分頁頁碼
            public int Page { get; set; }
            //顯示筆數
            public int Rows { get; set; }
        }
        public class FetchUserResult
        {
            [JsonPropertyName("id")]
            public string? ID { get; set; }
            [JsonPropertyName("account")]
            public string? Account { get; set; }
            [JsonPropertyName("name")]
            public string? Name { get; set; }
            [JsonPropertyName("email")]
            public string? Email { get; set; }
        }
        [HttpPost]
        public async Task<IActionResult> FetchUser([FromBody] FetchUserPara info)
        {
            await Task.Yield();
            try
            {
                //第一次載入: 空值
                if ((info.Values == null || info.Values.Count() == 0) && info.Page == 0)
                {
                    return new SystemTextJsonResult(new Select2ProcessResults
                    {
                        Results = Enumerable.Empty<Select2Result>(),
                        Pagination = false,
                    });
                }
                //第一次載入: 有初始值
                else if (info.Values != null && info.Values.Any())
                {
                    var values = info.Values.Where(s => !string.IsNullOrEmpty(s)).Select(s => s);
                    var results = from a in values
                                  join b in _WebServerDBContext.User on a equals b.ID into temp1
                                  from b in temp1.DefaultIfEmpty()
                                  select new FetchUserResult
                                  {
                                      ID = a,
                                      Account = b == null ? "" : b.Account,
                                      Name = b == null ? a : b.Name, // 當找不到資料時, 顯示ID, 以便判斷錯誤
                                      Email = b == null ? "" : b.Email,
                                  };
                    return new SystemTextJsonResult(new Select2ProcessResults
                    {
                        Results = results.Select(s => new Select2Result
                        {
                            ID = s.ID,
                            Text = System.Text.Json.JsonSerializer.Serialize(s), //轉成Json字串, 方便傳遞參數
                        }),
                        Pagination = false,
                    });
                }
                //查詢
                else
                {
                    //後續資料比對都用小寫
                    info.Parameter = (info.Parameter ?? "").ToLower();

                    var results = from a in _WebServerDBContext.User
                                  where a.Account.ToLower().Contains(info.Parameter)
                                    || a.Name.ToLower().Contains(info.Parameter)
                                    || a.Email.ToLower().Contains(info.Parameter)
                                  orderby a.Account
                                  select new FetchUserResult
                                  {
                                      ID = a.ID,
                                      Account = a.Account,
                                      Name = a.Name,
                                      Email = a.Email,
                                  };
                    //總筆數
                    var nTotalCount = await results.CountAsync();
                    //要顯示的起始筆數
                    var start = (info.Page - 1) * info.Rows;
                    //顯示的筆數
                    var r = await results.Skip(start).Take(info.Rows).ToListAsync();
                    //是否還有資料
                    var p = (nTotalCount - start) > info.Rows;

                    return new SystemTextJsonResult(new Select2ProcessResults
                    {
                        Results = r.Select(s => new Select2Result
                        {
                            ID = s.ID,
                            Text = System.Text.Json.JsonSerializer.Serialize(s), //轉成Json字串, 方便傳遞參數
                        }),
                        Pagination = p
                    });
                }
            }
            catch (Exception e)
            {
                return new SystemTextJsonResult(new Select2ProcessResults
                {
                    Results = Enumerable.Empty<Select2Result>(),
                    Pagination = false,
                    ErrorMessage = e.Message,
                });
            }
        }
        #endregion
    }
}
