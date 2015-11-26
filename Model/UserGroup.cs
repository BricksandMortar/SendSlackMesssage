using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.bricksandmortarstudio.Slack.Model
{
    public class UserGroup
    {
        public string id { get; set; }
        public string team_id { get; set; }
        public bool is_usergroup { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string handle { get; set; }
        public bool is_external { get; set; }
        public int date_create { get; set; }
        public int date_update { get; set; }
        public int date_delete { get; set; }
        public string auto_type { get; set; }
        public string created_by { get; set; }
        public string updated_by { get; set; }
        public object deleted_by { get; set; }
        public Preferences prefs { get; set; }
        public List<string> users { get; set; }
        public string user_count { get; set; }
    }
}
