using System.Text.Json.Serialization;

namespace WebServer.Models
{
    public class DataTableData
    {
        [JsonPropertyName("draw")]
        public int Draw { get; set; }
        [JsonPropertyName("data")]
        public object? Data { get; set; }
        [JsonPropertyName("recordsTotal")]
        public int RecordsTotal { get; set; }
        [JsonPropertyName("recordsFiltered")]
        public int RecordsFiltered { get; set; }
    }
}