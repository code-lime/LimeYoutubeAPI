using System;
using System.Collections.Generic;
using System.Text;

namespace LimeYoutubeAPI.Base
{
    internal static class YoutubeURL
    {
        internal static Uri GetVideoInfo(string videoID) => new Uri($"{'h'}ttps://www.youtube.com/get_video_info?video_id={videoID}");
        internal static Uri GetLiveChat(string videoID) => new Uri($"{'h'}ttps://www.youtube.com/live_chat?v={videoID}");
        internal static Uri GetLiveChatContinuation(string continuation) => new Uri($"{'h'}ttps://www.youtube.com/live_chat?continuation={continuation}");
        internal static Uri GetChannel(string channelID) => new Uri($"{'h'}ttps://www.youtube.com/channel/{channelID}");
        internal static Uri GetAvatar(string avatarID, int size) => new Uri($"{'h'}ttps://yt4.ggpht.com/ytc/{avatarID}=s{size}");
    }
}
