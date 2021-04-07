using System;
using System.Collections.Generic;
using System.Text;

namespace LimeYoutubeAPI.Live
{
    public class ChatMessage
    {
        public ChatChannel Author { get; }
        public string Context { get; }
        public DateTime UtcTime { get; }
        public string MessageID { get; }

        internal ChatMessage(ChatChannel author, string context, DateTime utcTime, string messageID)
        {
            Author = author;
            Context = context;
            UtcTime = utcTime;
            MessageID = messageID;
        }
    }
}
