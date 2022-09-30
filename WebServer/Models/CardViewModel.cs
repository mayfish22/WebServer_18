using System.ComponentModel.DataAnnotations;
using WebServer.Models.WebServerDB;
using WebServer.Services;

namespace WebServer.Models
{
    public class CardViewModel
    {
        public Card Card { get; set; }
        public string? ErrorMessage { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            using (var serviceScope = ServiceActivator.GetScope())
            {
                ValidatorService? validatorService = (ValidatorService?)serviceScope.ServiceProvider.GetService(typeof(ValidatorService));

                var rs = validatorService?.ValidateCard(Card);
                if (rs != null && rs.Any())
                {
                    var x = rs.Select(r => new ValidationResult(r.Text, new[] { string.IsNullOrEmpty(r.ElementID) ? nameof(ErrorMessage) : r.ElementID }));
                    return x;
                }
            }
            return Enumerable.Empty<ValidationResult>();
        }
    }
}