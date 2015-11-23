using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace com.bricksandmortarstudio.Slack
{
    //https://api.slack.com/docs/attachments
    public class Attachment
    {
        public string fallback { get; set; }
        public string color { get; set; }
        public string pretext { get; set; }

        public string author_name { get; set; }
        public string author_link { get; set; }
        public string author_icon { get; set; }

        public string title { get; set; }
        public string title_link { get; set; }

        public string text { get; set; }

        public Field[] fields { get; set; }

        public string image_url { get; set; }

        public string thumb_url { get; set; }


    }

}
