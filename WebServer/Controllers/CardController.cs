using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServer.Extensions;
using WebServer.Models;
using WebServer.Models.WebServerDB;
using WebServer.Services;

namespace WebServer.Controllers
{
    [Authorize]
    [ServiceFilter(typeof(WebServer.Filters.AuthorizeFilter))]
    public class CardController : Controller
    {
        private readonly ILogger<CardController> _logger;
        private readonly WebServerDBContext _WebServerDBContext;
        private readonly SiteService _SiteService;

        public CardController(ILogger<CardController> logger,
            WebServerDBContext WebServerDBContext,
            SiteService SiteService)
        {
            _logger = logger;
            _WebServerDBContext = WebServerDBContext;
            _SiteService = SiteService;
        }

        #region Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await Task.Yield();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetColumns()
        {
            try
            {
                var _columnList = new List<string>
                {
                    nameof(CardIndexViewModel.CardNo),
                    nameof(CardIndexViewModel.UserName),
                    nameof(CardIndexViewModel.UserEmail),
                };
                var columns = await _SiteService.GetDatatableColumns<CardIndexViewModel>(_columnList);

                return new SystemTextJsonResult(new
                {
                    status = "success",
                    data = columns,
                });
            }
            catch (Exception e)
            {
                return new SystemTextJsonResult(new
                {
                    status = "fail",
                    message = e.Message,
                });
            }
        }

        /// <summary>
        /// For Data Table
        /// </summary>
        /// <param name="draw">DataTable用,不用管他</param>
        /// <param name="start">起始筆數</param>
        /// <param name="length">顯示筆數</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetData(int draw, int start, int length)
        {
            await Task.Yield();
            try
            {
                //總筆數
                int nTotalCount = await _WebServerDBContext.Card.CountAsync();

                var info = from n1 in _WebServerDBContext.Card
                           //---------------------------------------------
                           // left join
                           join n2 in _WebServerDBContext.User on n1.UserID equals n2.ID into tempN2
                           from n2 in tempN2.DefaultIfEmpty()
                           //----------------------------------------------
                           select new CardIndexViewModel
                           {
                               ID = n1.ID,
                               CardNo = n1.CardNo,
                               UserName = n2 == null ? string.Empty : n2.Name,
                               UserEmail = n2 == null ? string.Empty : n2.Email,
                           };

                #region 關鍵字搜尋
                if (!string.IsNullOrEmpty((string)Request.Form["search[value]"]))
                {
                    string sQuery = Request.Form["search[value]"].ToString().ToUpper();
                    bool IsNumber = decimal.TryParse(sQuery, out decimal nQuery);
                    info = info.Where(t =>
                                 (!string.IsNullOrEmpty(t.CardNo) && t.CardNo.ToUpper().Contains(sQuery))
                                || (!string.IsNullOrEmpty(t.UserName) && t.UserName.ToUpper().Contains(sQuery))
                                || (!string.IsNullOrEmpty(t.UserEmail) && t.UserEmail.ToUpper().Contains(sQuery))
                    );
                }
                #endregion 關鍵字搜尋

                #region 排序
                int sortColumnIndex = (string)Request.Form["order[0][column]"] == null ? -1 : int.Parse(Request.Form["order[0][column]"]);
                string sortDirection = (string)Request.Form["order[0][dir]"] == null ? "" : Request.Form["order[0][dir]"].ToString().ToUpper();
                string sortColumn = Request.Form["columns[" + sortColumnIndex + "][data]"].ToString() ?? "";

                bool bDescending = sortDirection.Equals("DESC");
                switch (sortColumn)
                {
                    case nameof(CardIndexViewModel.CardNo):
                        info = bDescending ? info.OrderByDescending(o => o.CardNo) : info.OrderBy(o => o.CardNo);
                        break;

                    case nameof(CardIndexViewModel.UserName):
                        info = bDescending ? info.OrderByDescending(o => o.UserName) : info.OrderBy(o => o.UserName);
                        break;
                    case nameof(CardIndexViewModel.UserEmail):
                        info = bDescending ? info.OrderByDescending(o => o.UserEmail) : info.OrderBy(o => o.UserEmail);
                        break;

                    default:
                        info = info.OrderBy(o => o.CardNo);
                        break;
                }

                #endregion 排序

                //結果
                var list = nTotalCount == 0 ? new List<CardIndexViewModel>() : info.Skip(start).Take(Math.Min(length, nTotalCount - start)).ToList();

                return new SystemTextJsonResult(new DataTableData
                {
                    Draw = draw,
                    Data = list,
                    RecordsTotal = nTotalCount,
                    RecordsFiltered = info.Count()
                });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        #endregion

        #region Edit
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                return View(await GetCardViewModelAsync(id));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CardViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.Where(s => s.Errors.Any()).Select(s => s);
                    throw new Exception(errors.First().Errors.First().ErrorMessage);
                }
                var card = await _WebServerDBContext.Card.FindAsync(model.Card?.ID);
                if (card == null)
                    throw new Exception("卡片不存在");
                card.UserID = model.Card?.UserID;
                await _WebServerDBContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                ModelState.AddModelError(nameof(CardViewModel.ErrorMessage), e.Message);
                return View(model);
            }
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Details
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                return View(await GetCardViewModelAsync(id));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        #endregion

        #region Delete
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                return View(await GetCardViewModelAsync(id));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpPost, ActionName(nameof(Delete))]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await Task.Yield();
            try
            {
                var cardHistorys = _WebServerDBContext.CardHistory.Where(s => s.CardID == id).Select(s => s);
                var card = await _WebServerDBContext.Card.FindAsync(id);
                if (card == null)
                    throw new Exception("卡片不存在");
                if (cardHistorys != null && cardHistorys.Any())
                    _WebServerDBContext.CardHistory.RemoveRange(cardHistorys);
                _WebServerDBContext.Card.Remove(card);
                await _WebServerDBContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                ModelState.AddModelError(nameof(CardViewModel.ErrorMessage), e.Message);
                return View(await GetCardViewModelAsync(id));
            }
            return RedirectToAction(nameof(Index));
        }
        #endregion

        public async Task<CardViewModel> GetCardViewModelAsync(string id)
        {
            await Task.Yield();
            CardViewModel model;
            var card = await _WebServerDBContext.Card.FindAsync(id);
            if (card == null)
            {
                model = new CardViewModel
                {
                    Card = new Card
                    {
                        ID = Guid.NewGuid().ToString(),
                    },
                };
            }
            else
            {
                model = new CardViewModel
                {
                    Card = card,
                };
            }

            return model;
        }
    }
}