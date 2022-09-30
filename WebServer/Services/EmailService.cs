using SendGrid;
using SendGrid.Helpers.Mail;
using WebServer.Models;

namespace WebServer.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _senderEmail;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _senderEmail = configuration.GetValue<string>("SendGrid:SenderEmail");
        }
        private SendGridClient Client()
        {
            var apiKey = _configuration.GetValue<string>("SendGrid:ApiKey");
            var client = new SendGridClient(apiKey);
            return client;
        }

        /// <summary>
        /// 寄信
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<Response> Send(MailModel model)
        {
            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(
                new EmailAddress(_senderEmail),
                model.Receivers.ToList(),
                model.Subject,
                model.PlainTextContent,
                model.HtmlContent);
            if (model.CCs.Any())
                msg.AddCcs(model.CCs.ToList());
            if (model.Attachments.Any())
                msg.AddAttachments(model.Attachments.ToList());
            // Send Mail
            return await Client().SendEmailAsync(msg);
        }
    }
}