using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.bricksandmortarstudio.Slack.Model.Channel
{
    public class Topic
    {
        public string value { get; set; }
        public string creator { get; set; }
        public int last_set { get; set; }
    }
}
