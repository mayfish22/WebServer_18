using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models.WebServerDB
{
    [ModelMetadataType(typeof(MenuMetadata))]
    public partial class Menu
    {
        [NotMapped]
        [Display(Name = "Menu.Name")]
        public string? Name { get; set; }
        [NotMapped]
        [Display(Name = "Description")]
        public string? Description { get; set; }
        [NotMapped]
        public string? IDs { get; set; }
        [NotMapped]
        public long Depth { get; set; }
        // For Hierarchy 
        [NotMapped]
        public Guid GID { get; set; }
        // For Hierarchy 
        [NotMapped]
        public Guid? GPID { get; set; }
    }
    public partial class MenuMetadata
    {

    }
}