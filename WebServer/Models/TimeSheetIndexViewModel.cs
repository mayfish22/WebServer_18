using System.ComponentModel.DataAnnotations;

namespace WebServer.Models
{
    public class TimeSheetIndexViewModel
    {
        public string? ID { get; set; }
        [Display(Name = "CardNo")]
        public string? CardNo { get; set; }
        [Display(Name = "Name")]
        public string? UserName { get; set; }
        [Display(Name = "PunchInDateTime")]
        public string? PunchInDateTime { get; set; }
    }
}