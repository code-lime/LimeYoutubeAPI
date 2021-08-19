using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using SpanParser.Json;

namespace LimeYoutubeAPI.Live
{
    public class ChatSponsor : BaseChatElement
    {
        public virtual string SponsorID { get; }
        public ChatSponsor(ChatChannel author, DateTime utcTime, string sponsorID, string messageID) : base(author, utcTime, messageID) {
            SponsorID = sponsorID;
        }
        internal ChatSponsor(JSpan json) : base(json) {
            SponsorID = getSponsorID(json);
        }
        private static string getSponsorID(JSpan chatItem)
        {
            var text = chatItem["headerSubtext"]["runs"][1]["text"];
            return text.IsEmpty ? "none" : text.ToString();
        }
    }
}
