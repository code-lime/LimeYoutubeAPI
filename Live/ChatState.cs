using System;
using System.Collections.Generic;
using System.Text;

namespace LimeYoutubeAPI.Live
{
    public class ChatState
    {
        public int Code { get; }
        public string Data { get; }

        public ChatState(int code, string data)
        {
            Code = code;
            Data = data;
        }
    }
}
