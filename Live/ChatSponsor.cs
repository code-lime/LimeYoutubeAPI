using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LimeYoutubeAPI.Live
{
    public class ChatSponsor : BaseChatElement
    {
        public ChatSponsor(ChatChannel author, DateTime utcTime, string messageID) : base(author, utcTime, messageID) { }
        internal ChatSponsor(JToken json) : base(json) { }
    }
}
