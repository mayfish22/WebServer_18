namespace WebServer.Models
{
    public enum SortingTypeEnum
    {
        Disabled,
        Enabled,
    }
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public class SortingTypeAttribute : Attribute
    {
        private readonly SortingTypeEnum _Type;
        private readonly string[] _Parameters = Array.Empty<string>();
        public SortingTypeEnum Type
        {
            get { return _Type; }
        }
        public string[] Parameters
        {
            get { return _Parameters; }
        }
        public SortingTypeAttribute()
        {
            _Type = SortingTypeEnum.Enabled;
        }
        public SortingTypeAttribute(SortingTypeEnum type)
        {
            _Type = type;
        }
        public SortingTypeAttribute(SortingTypeEnum type, string[] Parameters)
        {
            _Type = type;
            _Parameters = Parameters;
        }
    }
}