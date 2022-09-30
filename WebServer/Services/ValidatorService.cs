using WebServer.Models.WebServerDB;

namespace WebServer.Services
{
    public class ValidatorMessage
    {
        public string? ElementID { get; set; } = string.Empty;
        public string? Text { get; set; }
    }
    public class ValidatorService
    {
        private readonly WebServerDBContext _WebServerDBContext;
        public ValidatorService(WebServerDBContext WebServerDBContext)
        {
            _WebServerDBContext = WebServerDBContext;
        }
        /// <summary>
        /// SignupViewModel資料驗證
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public IEnumerable<ValidatorMessage> ValidateSignup(User user)
        {
            var result = new List<ValidatorMessage>();

            //檢查帳號是否重覆
            var accountDupes = _WebServerDBContext.User
                .Where(s => (string.IsNullOrEmpty(user.ID) || s.ID.ToUpper() != user.ID.ToUpper())
                    && s.Account.ToUpper() == user.Account.Trim().ToUpper())
                .Select(s => s);
            if (accountDupes.Any())
            {
                result.Add(new ValidatorMessage
                {
                    ElementID = "User.Account",
                    Text = "帳號已被使用",
                });
            }

            //檢查Email是否重覆
            var emailDupes = _WebServerDBContext.User
                .Where(s => (string.IsNullOrEmpty(user.ID) || s.ID.ToUpper() != user.ID.ToUpper())
                    && s.Email.ToUpper() == user.Email.Trim().ToUpper())
                .Select(s => s);
            if (emailDupes.Any())
            {
                result.Add(new ValidatorMessage
                {
                    ElementID = "User.Email",
                    Text = "電子信箱已被使用",
                });
            }
            return result;
        }

        /// <summary>
        /// CardViewModel資料驗證
        /// </summary>
        /// <param name="card"></param>
        /// <returns></returns>
        public IEnumerable<ValidatorMessage> ValidateCard(Card? card)
        {
            var result = new List<ValidatorMessage>();

            if(card == null)
            {
                result.Add(new ValidatorMessage
                {
                    Text = $"卡片異常",
                });
            }
            else
            {
                //檢查卡號是否重覆
                var cardNoDupes = _WebServerDBContext.Card.Where(s => (string.IsNullOrEmpty(card.ID) || s.ID.ToLower() != card.ID.ToLower()) && s.CardNo.ToLower() == card.CardNo.Trim().ToLower()).Select(s => s);
                if (cardNoDupes.Any())
                {
                    result.Add(new ValidatorMessage
                    {
                        ElementID = "Card.CardNo",
                        Text = $"卡號重覆",
                    });
                }
            }
            return result;
        }
    }
}