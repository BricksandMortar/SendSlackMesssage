namespace com.bricksandmortarstudio.Slack.Model
{
    public class User
    {
        public string id { get; set; }
        public string name { get; set; }
        public bool deleted { get; set; }
        public string color { get; set; }
        public Profile profile { get; set; }
        public bool is_admin { get; set; }
        public bool is_owner { get; set; }
        public bool is_primary_owner { get; set; }
        public bool is_restricted { get; set; }
        public bool is_ultra_restricted { get; set; }
        public bool has_2fa { get; set; }
        public string two_factor_type { get; set; }
        public bool has_files { get; set; }
    }
}

