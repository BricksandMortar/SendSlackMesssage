using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.bricksandmortarstudio.Slack
{
    public class Message
    {   
        public string token { get; set; }
        public string text { get; set; }
        public string username { get; set; }
        public string icon_url { get; set; }
        public string icon_emoji { get; set; }
        public string channel { get; set; }

        public bool ShouldSerializeusername()
        {
            if ( string.IsNullOrEmpty( username ) )
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ShouldSerializeicon_url()
        {
            if ( string.IsNullOrEmpty( icon_url ) )
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ShouldSerializeicon_emoji()
        {
            if ( string.IsNullOrEmpty( icon_emoji ) )
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ShouldSerializechannel()
        {
            if ( string.IsNullOrEmpty( channel ) )
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
