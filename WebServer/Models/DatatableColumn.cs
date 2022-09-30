using System.Text.Json.Serialization;

namespace WebServer.Models
{
    public class DatatableColumn
    {
        [JsonPropertyName("seq")]
        public long? Seq { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
        [JsonPropertyName("isVisible")]
        public bool? IsVisible { get; set; }
        [JsonPropertyName("displayType")]
        public string? DisplayType { get; set; }
        [JsonPropertyName("displayParameters")]
        public string[]? DisplayParameters { get; set; }
        [JsonPropertyName("sortingType")]
        public string? SortingType { get; set; }
        [JsonPropertyName("sortingParameters")]
        public string[]? SortingParameters { get; set; }
    }
}