using System;
using System.Collections.Generic;
using System.Text;

namespace LimeYoutubeAPI.Models
{
    public class YoutubeStream : YoutubeVideo
    {
        public virtual DateTime StartDate { get; }
        public override bool IsLive => true;

        internal YoutubeStream(string videoID, string channelID, string channelName, Uri preview, long views, string title, string description, TimeSpan videoLength, DateTime startDate) : base(videoID, channelID, channelName, preview, views, title, description, videoLength)
        {
            StartDate = startDate;
        }
    }
}
