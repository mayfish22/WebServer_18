using SendGrid.Helpers.Mail;

namespace WebServer.Models
{
    public class MailModel
    {
        //收件人
        public IEnumerable<EmailAddress> Receivers { get; set; } = Enumerable.Empty<EmailAddress>();
        //信件主旨
        public string? Subject { get; set; }
        //純文字內容
        public string? PlainTextContent { get; set; }
        //網頁格式內容
        public string? HtmlContent { get; set; }
        //副本
        public IEnumerable<EmailAddress> CCs { get; set; } = Enumerable.Empty<EmailAddress>();
        //夾帶檔案
        public IEnumerable<Attachment> Attachments { get; set; } = Enumerable.Empty<Attachment>();
    }
}