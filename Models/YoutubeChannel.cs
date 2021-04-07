using LimeYoutubeAPI.Base;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LimeYoutubeAPI.Models
{
    public class YoutubeChannel : BaseChannel
    {
        public override string ChannelID { get; }
        public override string UserName { get; }
        public override string AvatarID { get; }
        public virtual string StreamID { get; }

        internal YoutubeChannel(string channelID, string userName, string avatarID, string streamID)
        {
            ChannelID = channelID;
            UserName = userName;
            AvatarID = avatarID;
            StreamID = streamID;
        }
    }
}
