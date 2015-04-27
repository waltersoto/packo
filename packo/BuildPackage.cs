
namespace packo
{

    public class BuildPackage
    {
        public string Package { get; set; }
        public string Version { get; set; }
        public string Source { get; set; }
        public string Release { get; set; }
        public Action[] Actions { get; set; }
    }

    public class Action
    {
        public string Type { get; set; }
        public string Filename { get; set; }
        public string[] Ignore { set; get; }
        public Setting[] Settings { get; set; }
    }

    public class Setting
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
