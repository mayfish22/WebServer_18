using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebServer.Models;
using WebServer.Models.WebServerDB;
using WebServer.Services;

namespace WebServer.Controllers
{
    public class AccountController : Controller
    {
        private readonly WebServerDBContext _WebServerDBContext;
        private readonly SiteService _SiteService;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IViewRenderService _viewRenderService;
        private readonly EmailService _emailService;
        private readonly JWTService _jwtService;
        public AccountController(WebServerDBContext WebServerDBContext
            , SiteService SiteService
            ,IHttpContextAccessor httpContext
            , IViewRenderService viewRenderService
            , EmailService emailService
            , JWTService jwtService)
        {
            _WebServerDBContext = WebServerDBContext;
            _SiteService = SiteService;
            _httpContext = httpContext;
            _viewRenderService = viewRenderService;
            _emailService = emailService;
            _jwtService = jwtService;
        }

        [HttpGet]
        public async Task<IActionResult> Signin(string returnUrl)
        {
            await Task.Yield();
            var model = new SigninViewModel
            {
                //登入後要轉跳的頁面
                ReturnUrl = returnUrl,
            };
            return View(model);
        }

        [HttpPost]
        //防止 CSRF (Cross-Site Request Forgery) 跨站偽造請求的攻擊
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signin(SigninViewModel model)
        {
            try
            {
                //檢查帳號密碼是否正確
                //通常帳號會忽略大小寫
                if (string.IsNullOrEmpty(model.Account))
                {
                    throw new Exception("請輸入帳號");
                }
                if (string.IsNullOrEmpty(model.Password))
                {
                    throw new Exception("請輸入密碼");
                }

                //允許 Account 或 Email 登入
                var query = from s in _WebServerDBContext.User
                            where (s.Account.ToUpper() == model.Account.Trim().ToUpper()
                                 || s.Email.ToUpper() == model.Account.Trim().ToUpper())
                                && s.Password == _SiteService.EncoderSHA512(model.Password)
                            select s;

                if (query == null || !query.Any())
                    throw new Exception("帳號或密碼錯誤");

                if (query.FirstOrDefault().IsEnabled == 0)
                    throw new Exception("帳號停用");

                // 將使用者資訊記錄到 Session 中
                await _SiteService.SetUserProfile(query.First().ID);

                // 設定 Cookie
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, model.Account.Trim().ToUpper()),
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(principal);

                //沒有指定返回的頁面就導向 /Home/Index
                if (string.IsNullOrEmpty(model.ReturnUrl))
                    return RedirectToAction("Index", "Home");
                else
                    return Redirect(model.ReturnUrl);
            }
            catch (Exception e)
            {
                //錯誤訊息
                ModelState.AddModelError(nameof(SigninViewModel.ErrorMessage), e.Message);
                return View(nameof(Signin), model);
            }
        }

        [HttpGet, HttpPost]
        public async Task<IActionResult> Signout([FromQuery] string ReturnUrl)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            foreach (var cookie in HttpContext.Request.Cookies)
            {
                Response.Cookies.Delete(cookie.Key);
            }
            HttpContext.Session.Remove("CurrentUser");
            HttpContext.Session.Clear();
            return RedirectToAction("Signin", "Account", new
            {
                returnUrl = ReturnUrl
            });
        }

        [HttpGet]
        public async Task<IActionResult> Signup()
        {
            await Task.Yield();
            var model = new SignupViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(SignupViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.Where(s => s.Errors.Any()).Select(s => s);
                    throw new Exception(errors.First().Errors.First().ErrorMessage);
                }
                if(model.User != null)
                {
                    model.User.ID = Guid.NewGuid().ToString().ToUpper();
                    model.User.Account = model.User.Account.Trim();
                    model.User.Password = _SiteService.EncoderSHA512(model.User.Password);
                    model.User.Name = model.User.Name.Trim();
                    model.User.Email = model.User.Email.Trim().ToUpper();
                    model.User.IsEnabled = 1;
                }

                await _WebServerDBContext.User.AddAsync(model.User);
                await _WebServerDBContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                ModelState.AddModelError(nameof(SignupViewModel.ErrorMessage), e.Message);
                return View(model);
            }
            //返回登入頁, 並自動代入所註冊的帳號
            return View(nameof(Signin), new SigninViewModel
            {
                Account = model.User.Account,
            });
        }

        [HttpGet]
        public async Task<IActionResult> ForgotPassword()
        {
            await Task.Yield();
            var model = new ForgotPasswordViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.Where(s => s.Errors.Any()).Select(s => s);
                    throw new Exception(errors.First().Errors.First().ErrorMessage);
                }

                //檢查EMAIL是否存在
                var user = (from a in _WebServerDBContext.User
                            where a.Email.ToLower() == model.Email.Trim().ToLower()
                            select a)?.FirstOrDefault();
                if (user == null)
                {
                    throw new Exception($"找不到此信箱：{model.Email}");
                }

                //重置密碼信件有效時間
                var expiryMinutes = 30;
                var forgotPassword = new ForgotPassword
                {
                    ID = Guid.NewGuid().ToString(),
                    UserID = user.ID,
                    IsReseted = 0,
                    ExpiryDateTime = DateTime.Now.AddMinutes(expiryMinutes).ToString("yyyy-MM-dd HH:mm:ss.fff"),
                };
                await _WebServerDBContext.ForgotPassword.AddAsync(forgotPassword);
                await _WebServerDBContext.SaveChangesAsync();

                var content = $"{user.Name}，您好";
                content += $"<br/>此連結{expiryMinutes}分鐘內有效";
                content += $"<p/><p/>";

                //信件內容
                var tmp = new NotificationMailViewModel
                {
                    Title = $"重置密碼",
                    Preheader = $"連結 {expiryMinutes}分鐘內有效。",
                    ActionUrl = $"{_httpContext?.HttpContext?.Request.Scheme}://{_httpContext?.HttpContext?.Request.Host}/Account/ResetPassword/{forgotPassword.ID}",
                    ActionText = $"重置密碼",
                    Content = content,
                };
                string template = await _viewRenderService.RenderToStringAsync("~/Views/Templates/NotificationMail.cshtml", tmp);

                //SendGrid寄信用
                var mailModel = new MailModel
                {
                    Subject = $"重置密碼",
                    PlainTextContent = $"{tmp.Preheader}\n{tmp.ActionUrl}",
                    HtmlContent = template,
                    Receivers = new List<SendGrid.Helpers.Mail.EmailAddress> { new SendGrid.Helpers.Mail.EmailAddress(model.Email) },
                };
                var response = await _emailService.Send(mailModel);
                if (!response.IsSuccessStatusCode)
                    throw new Exception("寄件失敗");

                return View("SentEmail");
            }
            catch (Exception e)
            {
                ModelState.AddModelError(nameof(ForgotPasswordViewModel.ErrorMessage), e.Message);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            try
            {
                var forgotPassword = await _WebServerDBContext.ForgotPassword.FindAsync(id);
                //找不到資料
                if (forgotPassword == null)
                    throw new Exception("無效的連結");
                //已經重置過
                if (forgotPassword.IsReseted == 1)
                    throw new Exception("失效的連結");
                //逾時
                if (DateTime.Parse(forgotPassword.ExpiryDateTime) < DateTime.Now)
                    throw new Exception("過期的連結");

                var model = new ResetPasswordViewModel
                {
                    ID = forgotPassword.UserID,
                };
                return View(model);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.Where(s => s.Errors.Any()).Select(s => s);
                    throw new Exception(errors.First().Errors.First().ErrorMessage);
                }
                var forgotPassword = await _WebServerDBContext.ForgotPassword.FindAsync(model.ID);
                forgotPassword.IsReseted = 1;
                //重設密碼
                var user = await _WebServerDBContext.User.FindAsync(forgotPassword.UserID);
                user.Password = _SiteService.EncoderSHA512(model.Password);
                await _WebServerDBContext.SaveChangesAsync();

                //返回登入頁
                return RedirectToAction("Signin", "Account");
            }
            catch (Exception e)
            {
                ModelState.AddModelError(nameof(ResetPasswordViewModel.ErrorMessage), e.Message);
                return View(model);
            }
        }

        [HttpGet, HttpPost]
        public async Task<IActionResult> SetLanguage(string culture, string returnUrl)
        {
            await Task.Yield();
            _SiteService.SetCulture(culture);
            return Redirect(returnUrl);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetToken([FromBody] SigninViewModel model)
        {
            await Task.Yield();
            try
            {
                //允許 Account 或 Email 登入
                var query = from s in _WebServerDBContext.User
                            where (s.Account.ToUpper() == model.Account.Trim().ToUpper()
                                 || s.Email.ToUpper() == model.Account.Trim().ToUpper())
                                && s.Password == _SiteService.EncoderSHA512(model.Password)
                            select s;

                if (query == null || !query.Any())
                    throw new Exception("帳號或密碼錯誤");
                var token = _jwtService.GenerateToken(query.First().ID);
                return Ok(token);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        #region Message
        public class MessagePara
        {
            public string? UserID { get; set; }
            public string? Title { get; set; }
            public string? Message { get; set; }
        }
        // https://localhost:7120/Account/Message
        [HttpPost]
        public async Task<IActionResult> Message([FromBody]MessagePara arg)
        {
            try
            {
                await _SiteService.Send(arg.UserID, arg.Title, arg.Message);
                return Ok();
            }
            catch(Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        #endregion
    }
}