using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace packo {
    public class Action {
        public string Type { get; set; }
        public string Filename { get; set; }
        public string[] Ignore { set; get; }
        public Setting[] Settings { get; set; }
    }
}
