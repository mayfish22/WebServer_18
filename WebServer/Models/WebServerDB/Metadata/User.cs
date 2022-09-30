using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models.WebServerDB
{
    [ModelMetadataType(typeof(UserMetadata))]
    public partial class User
    {
        [NotMapped]
        [Display(Name = "確認密碼")]
        [Required(ErrorMessage = "請再次輸入密碼")]
        [Compare("Password", ErrorMessage = "密碼不相符")]
        public string? ConfirmPassword { get; set; }
    }
    public partial class UserMetadata
    {
        [Display(Name = "ID")]
        public string? ID { get; set; }

        [Display(Name = "帳號")]
        [Required(ErrorMessage = "帳號必填")]
        //帳號字元限3~20碼，英文和數字(中間可包含一個【_】或【.】)。
        [RegularExpression(@"^(?=[^\._]+[\._]?[^\._]+$)[\w\.]{3,20}$", ErrorMessage = "帳號字元限3~20碼，英文和數字(中間可包含一個【_】或【.】)。")]
        public string? Account { get; set; }
        [Display(Name = "密碼")]
        [Required(ErrorMessage = "密碼必填")]
        //密碼限4~20個字
        [RegularExpression(@"^.{4,20}$", ErrorMessage = "密碼限4~20個字")]
        public string? Password { get; set; }
        [Display(Name = "電子信箱")]
        [Required(ErrorMessage = "電子信箱必填")]
        [EmailAddress(ErrorMessage = "無效的電子信箱格式")]
        [MaxLength(50)]
        public string? Email { get; set; }
        [Display(Name = "姓名")]
        [Required(ErrorMessage = "姓名必填")]
        [MaxLength(20)]
        public string? Name { get; set; }
        [Display(Name = "生日")]
        [Required(ErrorMessage = "生日必填")]
        public string? Birthday { get; set; }
    }
}