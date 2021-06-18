using LimeYoutubeAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LimeYoutubeAPI.Live
{
    public class ChatListener : IDisposable
    {
        private readonly YoutubeContext service;
        private readonly CancellationTokenSource canceller = new CancellationTokenSource();
        public YoutubeStream YoutubeStream { get; private set; }
        internal ChatListener(YoutubeContext service) => this.service = service;
        public event Action<ChatMessage> MessageEvent;
        public event Action<ChatSponsor> SponsorEvent;
        public event Action<ChatState> StateEvent;
        public Task Run(string videoID, TimeSpan? update = null) => RunTask(update ?? YoutubeService.DefaultUpdate, videoID, canceller.Token);
        public void Stop()
        {
            try { canceller.Cancel(); } catch { }
        }
        private async Task RunTask(TimeSpan updateTimeout, string videoID, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                YoutubeVideo video = await service.GetVideoAsync(videoID);
                token.ThrowIfCancellationRequested();
                if (!(video?.IsLive ?? false)) {
                    StateEvent?.Invoke(new ChatState(404, video == null ? "Video info not founded" : "Stream info not founded"));
                    return;
                }
                YoutubeLiveChatInfo liveChatInfo = await service.GetLiveChatInfoAsync(videoID);
                token.ThrowIfCancellationRequested();
                if (liveChatInfo == null) {
                    StateEvent?.Invoke(new ChatState(404, "LiveChat not founded"));
                    return;
                }
                YoutubeStream = (YoutubeStream)video;

                string lastMessageID = string.Empty;
                int errors = 0;
                DateTime utcInit = DateTime.UtcNow;
                token.ThrowIfCancellationRequested();
                StateEvent?.Invoke(new ChatState(200, $"LiveChat starded: {YoutubeStream.ChannelName}"));

                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    if (errors > 10)
                    {
                        StateEvent?.Invoke(new ChatState(400, "LiveChat closed"));
                        return;
                    }
                    await Task.Delay(updateTimeout, token);
                    token.ThrowIfCancellationRequested();
                    var chatElementsResponse = await service.GetNewChatElementsAsync(liveChatInfo.FullLiveChat, lastMessageID);
                    IEnumerable<BaseChatElement> elements = chatElementsResponse.Messages;
                    if (elements == null)
                    {
                        errors++;
                        continue;
                    }
                    foreach (var element in elements.Where
                        (element => 
                        { 
                        if (string.IsNullOrEmpty(lastMessageID) && element.UtcTime < utcInit) return false; 
                        return true; 
                        }))
                    {
                        token.ThrowIfCancellationRequested();
                        if (element is ChatMessage message) MessageEvent?.Invoke(message);
                        else if (element is ChatSponsor sponsor) SponsorEvent?.Invoke(sponsor);
                    }
                    errors = 0;
                    lastMessageID = chatElementsResponse.NewLastMessageId;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                StateEvent?.Invoke(new ChatState(401, e.ToString()));
            }
        }
        public void Dispose()
        {
            try { canceller.Cancel(); } catch { }
            Ext.UnregAll(ref MessageEvent);
            Ext.UnregAll(ref StateEvent);
            Ext.UnregAll(ref SponsorEvent);
        }
    }
}
