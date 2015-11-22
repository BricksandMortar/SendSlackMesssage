using Newtonsoft.Json;

namespace com.bricksandmortarstudio.Slack
{
    public class Field
    {
        public string title { get; set; }
        public string value { get; set; }
        [JsonProperty( PropertyName = "short" )]
        public bool flag { get; set; }
    }
}
