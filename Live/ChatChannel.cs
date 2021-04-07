using LimeYoutubeAPI.Base;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LimeYoutubeAPI.Live
{
    public class ChatChannel : BaseChannel
    {
        public override string ChannelID { get; }
        public override string UserName { get; }
        public override string AvatarID { get; }
        public virtual ChannelType Type { get; }

        internal ChatChannel(string channelID, string userName, string avatarID, ChannelType type)
        {
            ChannelID = channelID;
            UserName = userName;
            AvatarID = avatarID;
            Type = type;
        }
    }
}
