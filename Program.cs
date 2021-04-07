#if DEBUG
using System;
using LimeYoutubeAPI.Live;
using LimeYoutubeAPI.Base;
using LimeYoutubeAPI.Models;
using System.Threading.Tasks;
using System.IO;
using System.Text;

namespace LimeYoutubeAPI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            using (YoutubeService service = YoutubeService.Create())
            using (MultiChatListener listener = service.RunMultiChatListener())
            {
                listener.RunChannel("UCwKfmsba1g3SDcOzbU4zPXw", v => Listener_MessageEvent("ФУГА TV", v), v => Listener_StateEvent("ФУГА TV", v));
                listener.RunChannel("UC0rdUtnXyfzgQQz25wrkbSQ", v => Listener_MessageEvent("TEMP", v), v => Listener_StateEvent("TEMP", v));
                Task.WaitAll(listener.Task);
                /*ChatListener listener = await service.RunChatByChannel("UCwKfmsba1g3SDcOzbU4zPXw");
                if (listener == null) return;
                using (listener)
                {
                    listener.StateEvent += Listener_StateEvent;
                    listener.MessageEvent += Listener_MessageEvent;
                    Task.WaitAll(listener.Task);
                }*/
            }
        }

        private static void Listener_MessageEvent(string prefix, ChatMessage obj)
        {
            Console.WriteLine($"[{prefix}] [{obj.UtcTime.ToString("dd.MM.yyyy HH:mm:ss")}] {obj.Author.UserName}: {obj.Context}");
        }

        private static void Listener_StateEvent(string prefix, ChatState obj)
        {
            Console.WriteLine($"[{prefix}] [{obj.Code}] {obj.Data}");
        }
    }
}
#endif