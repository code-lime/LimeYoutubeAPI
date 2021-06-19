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
    internal static class Program
    {
        public static void MainMulit()
        {
            using (YoutubeService service = YoutubeService.Create())
            using (MultiChatListener listener = service.CreateMultiChatListener())
            {
                listener.RegisterChannel("UCSJ4gkVC6NrvII8umztf0Ow", v => Listener_MessageEvent("MULTI", v), v => Listener_SponsorEvent("MULTI", v), v => Listener_StateEvent("MULTI", v)).Wait();
                listener.Run().Wait();
            }
        }
        public static void MainSingle()
        {
            using (YoutubeService service = YoutubeService.Create())
            using (ChatListener listener = service.CreateChatListener())
            {
                listener.MessageEvent += (msg) => Listener_MessageEvent("SINGLE", msg);
                listener.SponsorEvent += (sponsor) => Listener_SponsorEvent("SINGLE", sponsor);
                listener.StateEvent += (state) => Listener_StateEvent("SINGLE", state);
                listener
                    .Run("5qap5aO4i9A")
                    .Wait();
            }
        }

        public static void Main(string[] args)
        {
            MainMulit();
            //MainSingle();
        }

        private static void Listener_MessageEvent(string prefix, ChatMessage obj)
        {
            Console.WriteLine($"[{prefix}] [{obj.UtcTime.ToString("dd.MM.yyyy HH:mm:ss")}] {obj.Author.UserName}: {obj.Context}");
        }
        
        private static void Listener_SponsorEvent(string prefix, ChatSponsor obj)
        {
            Console.WriteLine($"[{prefix}] [SPONSOR] [{obj.UtcTime.ToString("dd.MM.yyyy HH:mm:ss")}] {obj.Author.UserName}");
        }

        private static void Listener_StateEvent(string prefix, ChatState obj)
        {
            Console.WriteLine($"[{prefix}] [{obj.Code}] {obj.Data}");
        }
    }
}
#endif
