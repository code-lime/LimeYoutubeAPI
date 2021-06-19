using LimeYoutubeAPI.Models;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LimeYoutubeAPI.Live
{
    public class MultiChatListener : IDisposable
    {
        private readonly YoutubeContext service;
        private readonly CancellationTokenSource canceller = new CancellationTokenSource();
        public class DataStream
        {
            internal string lastMessageID = null;
            internal int errors = 0;
            internal DateTime utcInit = DateTime.UtcNow;
            public YoutubeStream Stream;
            public YoutubeLiveChatInfo LiveChat;
            public DataStream(YoutubeStream stream, YoutubeLiveChatInfo liveChat)
            {
                Stream = stream;
                LiveChat = liveChat;
            }
        }
        public ConcurrentDictionary<string, DataStream> YoutubeStreams { get; } = new ConcurrentDictionary<string, DataStream>();
        internal MultiChatListener(YoutubeContext service) => this.service = service;
        public event Action<string, ChatMessage> MessageEvent;
        public event Action<string, ChatSponsor> SponsorEvent;
        public event Action<string, ChatState> StateEvent;
        public Task Run(TimeSpan? update = null) => RunMultiTask(update ?? YoutubeService.DefaultUpdate, canceller.Token);
        public void RegisterVideo(string videoID, Action<ChatMessage> messageEvent = null, Action<ChatSponsor> sponsorEvent = null, Action<ChatState> stateEvent = null)
        {
            if (messageEvent != null) MessageEvent += (id, msg) => { if (id == videoID) messageEvent.Invoke(msg); };
            if (stateEvent != null) StateEvent += (id, msg) => { if (id == videoID) stateEvent.Invoke(msg); };
            if (sponsorEvent != null) SponsorEvent += (id, msg) => { if (id == videoID) sponsorEvent.Invoke(msg); };
            YoutubeStreams[videoID] = null;
        }
        public async Task RegisterChannel(string channelID, Action<ChatMessage> messageEvent = null, Action<ChatSponsor> sponsorEvent = null, Action<ChatState> stateEvent = null)
        {
            YoutubeChannel channel = await service.GetChannelAsync(channelID);
            if (channel.StreamID == null)
            {
                stateEvent?.Invoke(new ChatState(404, "Stream not founded"));
                return;
            }
            RegisterVideo(channel.StreamID, messageEvent, sponsorEvent, stateEvent);
        }
        private async Task RunMultiTask(TimeSpan updateTimeout, CancellationToken token)
        {
            try
            {
                await Task.Delay(updateTimeout);
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    foreach (var kv in YoutubeStreams.ToArray())
                    {
                        string videoID = kv.Key;
                        try
                        {
                            DataStream dataStream = kv.Value;
                            token.ThrowIfCancellationRequested();
                            if (dataStream == null)
                            {
                                YoutubeVideo video = await service.GetVideoAsync(videoID);
                                token.ThrowIfCancellationRequested();
                                if (!(video?.IsLive ?? false))
                                {
                                    if (YoutubeStreams.TryRemove(videoID, out _)) StateEvent?.Invoke(videoID, new ChatState(404, video == null ? "Video info not founded" : "Stream info not founded"));
                                    continue;
                                }
                                YoutubeLiveChatInfo liveChatInfo = await service.GetLiveChatInfoAsync(videoID);
                                token.ThrowIfCancellationRequested();
                                if (liveChatInfo == null)
                                {
                                    if (YoutubeStreams.TryRemove(videoID, out _)) StateEvent?.Invoke(videoID, new ChatState(404, "LiveChat not founded"));
                                    continue;
                                }
                                YoutubeStreams[videoID] = dataStream = new DataStream((YoutubeStream)video, liveChatInfo);
                                token.ThrowIfCancellationRequested();
                                StateEvent?.Invoke(videoID, new ChatState(200, $"LiveChat starded: {video.ChannelName}"));
                            }
                            token.ThrowIfCancellationRequested();
                            if (dataStream.errors > 10)
                            {
                                if (YoutubeStreams.TryRemove(videoID, out _)) StateEvent?.Invoke(videoID, new ChatState(400, "LiveChat closed"));
                                continue;
                            }
                            await Task.Delay(updateTimeout);
                            token.ThrowIfCancellationRequested();
                            var chatElementsResponse = await service.GetNewChatElementsAsync(dataStream.LiveChat.FullLiveChat, dataStream.lastMessageID);
                            IEnumerable<BaseChatElement> elements = chatElementsResponse.Messages;
                            if (elements == null)
                            {
                                dataStream.errors++;
                                continue;
                            }
                            foreach (var element in elements.Where
                                (element =>
                                {
                                    if (string.IsNullOrEmpty(dataStream.lastMessageID) && element.UtcTime < dataStream.utcInit) return false;
                                    return true;
                                }))
                            {
                                token.ThrowIfCancellationRequested();
                                if (element is ChatMessage message) MessageEvent?.Invoke(videoID, message);
                                else if (element is ChatSponsor sponsor) SponsorEvent?.Invoke(videoID, sponsor);
                            }
                            dataStream.errors = 0;
                            dataStream.lastMessageID = chatElementsResponse.NewLastMessageId;
                        }
                        catch (Exception e)
                        {
                            StateEvent?.Invoke(videoID, new ChatState(401, e.ToString()));
                        }
                        await Task.Delay(updateTimeout);
                    }
                }
            }
            catch (OperationCanceledException) { }
            {

            }
        }
        public void Dispose()
        {
            try { canceller.Cancel(); } catch { }
            Ext.UnregAll(ref MessageEvent);
            Ext.UnregAll(ref SponsorEvent);
            Ext.UnregAll(ref StateEvent);
        }
    }
}
