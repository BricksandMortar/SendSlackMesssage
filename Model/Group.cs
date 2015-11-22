using Newtonsoft.Json;

namespace com.bricksandmortarstudio.Slack
{
    public class Group
    {
        [JsonProperty( PropertyName = "Channel" )]
        public string name { get; set; }
        public string purpose { get; set; }
        public string topic { get; set; }

        public bool ShouldSerializepurpose()
        {
            if ( string.IsNullOrEmpty( purpose ) )
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ShouldSerializtopic()
        {
            if ( string.IsNullOrEmpty( topic ) )
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
