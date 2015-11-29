using System.Collections.Generic;
using Newtonsoft.Json;

using com.bricksandmortarstudio.Slack.Model.Channel;

namespace com.bricksandmortarstudio.Slack
{
    public class Group
    {
        public string id { get; set; }
        public string name { get; set; }
        public string is_group { get; set; }
        public int created { get; set; }
        public string creator { get; set; }
        public bool is_archived { get; set; }
        public bool is_mpim { get; set; }
        public List<string> members { get; set; }
        public Topic topic { get; set; }
        public Purpose purpose { get; set; }
        public string last_read { get; set; }
        public int unread_count { get; set; }
        public int unread_count_display { get; set; }
    }
}
