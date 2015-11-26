using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.bricksandmortarstudio.Slack.Model
{    public class Preferences
    {
        public List<Channel> channels { get; set; }
        public List<Group> groups { get; set; }
    }
}
