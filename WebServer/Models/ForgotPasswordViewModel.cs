using System.ComponentModel.DataAnnotations;

namespace WebServer.Models
{
    public class ForgotPasswordViewModel
    {
        [Display(Name = "電子信箱")]
        [Required(ErrorMessage = "電子信箱必填")]
        [EmailAddress(ErrorMessage = "無效的信件格式")]
        public string? Email { get; set; }
        public string? ErrorMessage { get; set; }
    }
}