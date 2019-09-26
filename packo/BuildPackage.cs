
namespace packo {

    public class BuildPackage {
        public string Package { get; set; }
        public string Version { get; set; }
        public string Source { get; set; }
        public string Release { get; set; }
        public Action[] Actions { get; set; }
    }
}
