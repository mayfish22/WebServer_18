using System.ComponentModel.DataAnnotations;

namespace WebServer.Models
{
    public class CardIndexViewModel
    {
        public string? ID { get; set; }
        [Display(Name = "CardNo")]
        public string? CardNo { get; set; }       
        [Display(Name = "Name")]
        public string? UserName { get; set; }
        [Display(Name = "Email")]
        public string? UserEmail { get; set; }
    }
}
