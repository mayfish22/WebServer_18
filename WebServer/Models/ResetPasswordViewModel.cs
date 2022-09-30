using System.ComponentModel.DataAnnotations;

namespace WebServer.Models
{
    public class ResetPasswordViewModel
    {
        public string? ID { get; set; }
        [Display(Name = "密碼")]
        [Required(ErrorMessage = "密碼必填")]
        //密碼限4~20個字
        [RegularExpression(@"^.{4,20}$", ErrorMessage = "密碼限4~20個字")]
        public string? Password { get; set; }
        [Display(Name = "確認密碼")]
        [Required(ErrorMessage = "請再次輸入密碼")]
        [Compare("Password", ErrorMessage = "密碼不相符")]
        public string? ConfirmPassword { get; set; }
        public string? ErrorMessage { get; set; }
    }
}