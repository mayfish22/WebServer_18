namespace WebServer.Models
{
    public class NotificationMailViewModel
    {
        //標題
        public string? Title { get; set; }
        //部分裝置預覽時會顯示
        public string? Preheader { get; set; }
        //按鈕連結
        public string? ActionUrl { get; set; }
        //按鈕文字
        public string? ActionText { get; set; }
        //內文
        public string? Content { get; set; }
    }
}