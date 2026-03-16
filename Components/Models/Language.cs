namespace VinhKhanhstreetfoods.Components.Models
{
    public class Language
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string NativeName { get; set; }
        public string Flag { get; set; }
        
        public Language()
        {
        }
        
        public Language(string code, string name, string nativeName, string flag = "")
        {
            Code = code;
            Name = name;
            NativeName = nativeName;
            Flag = flag;
        }
    }
}
