using System.ComponentModel.DataAnnotations;

namespace WebServer.Models
{
    public class UserIndexViewModel
    {
        public string? ID { get; set; }
        [Display(Name = "Account")]
        [SortingType(SortingTypeEnum.Disabled)]
        public string? Account { get; set; }
        [Display(Name = "Name")]
        public string? Name { get; set; }
        [Display(Name = "Birthday")]
        public string? Birthday { get; set; }
        [Display(Name = "Email")]
        public string? Email { get; set; }
    }
}