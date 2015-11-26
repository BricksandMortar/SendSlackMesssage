using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.bricksandmortarstudio.Slack.Model
{
    public class Im
    {
        public string id { get; set; }
        public bool is_im { get; set; }
        public string user { get; set; }
        public int created { get; set; }
        public bool is_user_deleted { get; set; }
    }
}
