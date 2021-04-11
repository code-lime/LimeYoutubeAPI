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
        private readonly YoutubeService service;
        private readonly CancellationTokenSource canceller = new CancellationTokenSource();
        public YoutubeStream YoutubeStream { get; private set; }
        internal ChatListener(YoutubeService service) => this.service = service;
        public event Action<ChatMessage> MessageEvent;
        public event Action<ChatState> StateEvent;
        public Task Run(string videoID, TimeSpan? update = null) => RunTask(update ?? YoutubeService.DefaultUpdate, videoID, canceller.Token);
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
                YoutubeLiveChatInfo liveChatInfo = await service.GetLiveChatInfo(videoID);
                token.ThrowIfCancellationRequested();
                if (liveChatInfo == null) {
                    StateEvent?.Invoke(new ChatState(404, "LiveChat not founded"));
                    return;
                }
                YoutubeStream = (YoutubeStream)video;

                string lastMessageID = null;
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
                    string firstMessageID = null;
                    IEnumerable<ChatMessage> messages = await service.GetChatMessages(liveChatInfo.FullLiveChat);
                    if (messages == null)
                    {
                        errors++;
                        continue;
                    }
                    foreach (var message in messages.Where(message => { if (lastMessageID == null && message.UtcTime < utcInit) return false; if (firstMessageID == null) firstMessageID = message.MessageID; return true; }).TakeWhile(message => lastMessageID != message.MessageID).Reverse())
                    {
                        token.ThrowIfCancellationRequested();
                        MessageEvent?.Invoke(message);
                    }
                    errors = 0;
                    lastMessageID = firstMessageID ?? lastMessageID;
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
        }
    }
}
