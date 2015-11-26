using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.bricksandmortarstudio.Slack.Model.Channel
{
    public class Latest
    {
        public string user { get; set; }
        public string old_name { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string subtype { get; set; }
        public string text { get; set; }
        public string ts { get; set; }
    }
}
