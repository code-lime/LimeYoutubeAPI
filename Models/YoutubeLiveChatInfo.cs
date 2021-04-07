using System;
using System.Collections.Generic;
using System.Text;

namespace LimeYoutubeAPI.Models
{
    public class YoutubeLiveChatInfo
    {
        public Uri FullLiveChat { get; }
        public Uri FilterLiveChat { get; }

        internal YoutubeLiveChatInfo(Uri fullLiveChat, Uri filterLiveChat)
        {
            FullLiveChat = fullLiveChat;
            FilterLiveChat = filterLiveChat;
        }
    }
}
