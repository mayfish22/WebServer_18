using System.ComponentModel.DataAnnotations;

namespace WebServer.Models
{
    public class SigninViewModel
    {
        [Display(Name = "Account")]
        [Required(ErrorMessage = "AccountRequired")]
        public string? Account { get; set; }
        [Display(Name = "密碼")]
        [Required(ErrorMessage = "密碼必填")]
        public string? Password { get; set; }
        public string? ReturnUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }
}