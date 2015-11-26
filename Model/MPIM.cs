using System.Collections.Generic;

using com.bricksandmortarstudio.Slack.Model.Channel;

namespace com.bricksandmortarstudio.Slack.Model
{
    public class MPIM
    {
        public string id { get; set; }
        public string name { get; set; }
        public bool is_mpim { get; set; }
        public bool is_group { get; set; }
        public int created { get; set; }
        public string creator { get; set; }
        public List<string> members { get; set; }
        public Latest latest { get; set; }
        public string last_read { get; set; }
        public int unread_count { get; set; }
        public int unread_count_display { get; set; }
    }
}
