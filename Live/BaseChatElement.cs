using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using SpanParser.Json;

namespace LimeYoutubeAPI.Live
{
    public class BaseChatElement
    {
        public ChatChannel Author { get; }
        public DateTime UtcTime { get; }
        public string MessageID { get; }

        public BaseChatElement(ChatChannel author, DateTime utcTime, string messageID)
        {
            Author = author;
            UtcTime = utcTime;
            MessageID = messageID;
        }
        internal BaseChatElement(JSpan json) : this(getChatChannel(json), getChatUtcTime(json), getChatMessageID(json)) { }

        private static string getChatMessageID(JSpan chatItem) => chatItem["id"].ToString();
        private static DateTime getChatUtcTime(JSpan chatItem)
        {
            long value = long.Parse(chatItem["timestampUsec"].ToString()) / 1000;
            return DateTimeOffset.FromUnixTimeMilliseconds(value).UtcDateTime;
        }
        private static ChatChannel getChatChannel(JSpan chatItem)
        {
            var _authorName = chatItem["authorName"]["simpleText"];
            string authorName = _authorName.IsEmpty ? "Unknown" : _authorName.ToString();
            ReadOnlySpan<char> authorIcon = chatItem["authorPhoto"]["thumbnails"].GetEnumerator().Last()["url"].ToSpan();
            string authorID = chatItem["authorExternalChannelId"].ToString();
            ChannelType authorType = ChannelType.None;

            var authorBadges = chatItem["authorBadges"].GetEnumerator();
            while (authorBadges.MoveNext())
            {
                var jbd = authorBadges.Current;
                var jjbd = jbd["liveChatAuthorBadgeRenderer"];
                var jjbdIcon = jjbd["icon"];
                if (!(jjbd.IsEmpty || jjbdIcon.IsEmpty))
                {
                    switch (jjbdIcon["iconType"].ToString())
                    {
                        case "VERIFIED": authorType |= ChannelType.Verified; break;
                        case "MODERATOR": authorType |= ChannelType.Moderator; break;
                        case "OWNER": authorType |= ChannelType.Owner; break;
                        default: authorType |= ChannelType.Other; break;
                    }
                }
                else
                {
                    authorType |= ChannelType.Sponsor;
                }
            }

            authorIcon = authorIcon[(authorIcon.LastIndexOf('/') + 1)..authorIcon.IndexOf('=')];
            return new ChatChannel(authorID, authorName, authorIcon.ToString(), authorType);
        }
    }
}
