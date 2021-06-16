using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using SpanParser.Json;

namespace LimeYoutubeAPI.Live
{
    public class ChatMessage : BaseChatElement
    {
        public string Context { get; }
        public ChatMessage(ChatChannel author, string context, DateTime utcTime, string messageID) : base(author, utcTime, messageID) => Context = context;
        internal ChatMessage(JSpan json) : base(json) => Context = json["message"]["runs"][0]["text"].ToString();

        public override string ToString()
        {
            return $"[{UtcTime}] {Author.UserName}: {Context}";
        }
    }
}
