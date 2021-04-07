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
        public static async Task Main(string[] args)
        {
            using (YoutubeService service = YoutubeService.Create())
            {
                ChatListener listener = await service.RunChatByChannel("UCwKfmsba1g3SDcOzbU4zPXw");
                if (listener == null) return;
                using (listener)
                {
                    listener.StateEvent += Listener_StateEvent;
                    listener.MessageEvent += Listener_MessageEvent;
                    Task.WaitAll(listener.Task);
                }
            }
        }

        private static void Listener_MessageEvent(ChatMessage obj)
        {
            Console.WriteLine($"[{obj.UtcTime.ToString("dd.MM.yyyy HH:mm:ss")}] {obj.Author.UserName}: {obj.Context}");
        }

        private static void Listener_StateEvent(ChatState obj)
        {
            Console.WriteLine($"[{obj.Code}] {obj.Data}");
        }
    }
}
#endif