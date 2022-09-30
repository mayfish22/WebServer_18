namespace WebServer.Models
{
    public enum DisplayTypeEnum
    {
        Text,
        Date,
        DateTime,
    }
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public class DisplayTypeAttribute : Attribute
    {
        private readonly DisplayTypeEnum _Type;
        private readonly string[] _Parameters = Array.Empty<string>();
        public DisplayTypeEnum Type
        {
            get { return _Type; }
        }
        public string[] Parameters
        {
            get { return _Parameters; }
        }
        public DisplayTypeAttribute()
        {
            _Type = DisplayTypeEnum.Text;
        }
        public DisplayTypeAttribute(DisplayTypeEnum type)
        {
            _Type = type;
        }
        public DisplayTypeAttribute(DisplayTypeEnum type, string[] Parameters)
        {
            _Type = type;
            _Parameters = Parameters;
        }
    }
}