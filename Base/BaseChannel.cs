using System;
using System.Collections.Generic;
using System.Text;

namespace LimeYoutubeAPI.Base
{
    public abstract class BaseChannel
    {
        public abstract string ChannelID { get; }
        public abstract string UserName { get; }
        public abstract string AvatarID { get; }

        public Uri GetAvatarURL(int size = 32) => YoutubeURL.GetAvatar(AvatarID, size);
    }
}
