using System;
using System.Collections.Generic;
using System.Text;

namespace LimeYoutubeAPI.Models
{
    public class YoutubeVideo
    {
        public virtual string VideoID { get; }
        public virtual string ChannelID { get; }
        public virtual string ChannelName { get; }
        public virtual Uri Preview { get; }
        public virtual long Views { get; }
        public virtual string Title { get; }
        public virtual string Description { get; }
        public virtual TimeSpan VideoLength { get; } 
        public virtual bool IsLive => false;

        internal YoutubeVideo(string videoID, string channelID, string channelName, Uri preview, long views, string title, string description, TimeSpan videoLength)
        {
            VideoID = videoID;
            ChannelID = channelID;
            ChannelName = channelName;
            Preview = preview;
            Views = views;
            Title = title;
            Description = description;
            VideoLength = videoLength;
        }
    }
}
