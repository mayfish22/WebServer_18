using CsvHelper.Configuration.Attributes;

namespace WebServer.Models
{
    public class TimeSheetReportModel
    {
        [Name("姓名")]
        [NameIndex(0)]
        public string? UserName { get; set; }
        [Name("日期")]
        [NameIndex(1)]
        public string? Date { get; set; }
        [Name("上班")]
        [NameIndex(2)]
        public string? PunchInTime { get; set; }
        [Name("下班")]
        [NameIndex(3)]
        public string? PunchOutTime { get; set; }
    }
}