using System;
using System.Collections.Generic;
using System.Text;

namespace LimeYoutubeAPI.Base
{
    public static class YoutubeURL
    {
        public static Uri GetVideoInfo(string videoID) => new Uri($"{'h'}ttps://www.youtube.com/get_video_info?video_id={videoID}");
        public static Uri GetLiveChat(string videoID) => new Uri($"{'h'}ttps://www.youtube.com/live_chat?v={videoID}");
        public static Uri GetLiveChatContinuation(string continuation) => new Uri($"{'h'}ttps://www.youtube.com/live_chat?continuation={continuation}");
        public static Uri GetChannel(string channelID) => new Uri($"{'h'}ttps://www.youtube.com/channel/{channelID}");
        public static Uri GetAvatar(string avatarID, int size) => new Uri($"{'h'}ttps://yt4.ggpht.com/ytc/{avatarID}=s{size}");
        public static Uri GetVideo(string videoID) => new Uri($"{'h'}ttps://www.youtube.com/watch?v={videoID}");
    }
}
