using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LimeYoutubeAPI.Live
{
    public class IChatElement
    {
        public ChatChannel Author { get; }
        public DateTime UtcTime { get; }
        public string MessageID { get; }

        public IChatElement(ChatChannel author, DateTime utcTime, string messageID)
        {
            Author = author;
            UtcTime = utcTime;
            MessageID = messageID;
        }
        internal IChatElement(JToken json) : this(getChatChannel(json), getChatUtcTime(json), getChatMessageID(json)) { }

        private static string getChatMessageID(JToken chatItem) => chatItem["id"].Value<string>();
        private static DateTime getChatUtcTime(JToken chatItem)
        {
            long value = chatItem["timestampUsec"].Value<long>() / 1000;
            return DateTimeOffset.FromUnixTimeMilliseconds(value).UtcDateTime;
        }
        private static ChatChannel getChatChannel(JToken chatItem)
        {
            string authorName = chatItem["authorName"]?["simpleText"]?.Value<string>() ?? "Unknown";
            string authorIcon = chatItem["authorPhoto"]?["thumbnails"]?.Last?["url"]?.Value<string>();
            string authorID = chatItem["authorExternalChannelId"].Value<string>();
            ChannelType authorType = ChannelType.None;
            foreach (var jbd in chatItem["authorBadges"]?.ToObject<JArray>() ?? new JArray())
            {
                JObject jjbd = jbd["liveChatAuthorBadgeRenderer"]?.ToObject<JObject>();
                if (jjbd != null && jjbd.TryGetValue("icon", out JToken icon))
                {
                    switch (icon?["iconType"]?.Value<string>())
                    {
                        case "VERIFIED": authorType |= ChannelType.Verified; break;
                        case "MODERATOR": authorType |= ChannelType.Moderator; break;
                        case "OWNER": authorType |= ChannelType.Owner; break;
                        default: authorType |= ChannelType.Other; break;
                    }
                }
                else authorType |= ChannelType.Sponsor;
            }
            authorIcon = authorIcon?[(authorIcon.LastIndexOf('/') + 1)..authorIcon.IndexOf('=')];
            return new ChatChannel(authorID, authorName, authorIcon, authorType);
        }
    }
}
