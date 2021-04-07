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
        private readonly YoutubeService service;
        private readonly CancellationTokenSource canceller = new CancellationTokenSource();
        public Task Task { get; }
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
        internal MultiChatListener(YoutubeService service, TimeSpan update)
        {
            this.service = service;
            Task = RunMultiTask(update, canceller.Token);
        }
        public event Action<string, ChatMessage> MessageEvent;
        public event Action<string, ChatState> StateEvent;
        public void RunVideo(string videoID, Action<ChatMessage> messageEvent = null, Action<ChatState> stateEvent = null)
        {
            if (messageEvent != null) MessageEvent += (id, msg) => { if (id == videoID) messageEvent.Invoke(msg); };
            if (stateEvent != null) StateEvent += (id, msg) => { if (id == videoID) stateEvent.Invoke(msg); };
            YoutubeStreams[videoID] = null;
        }
        public void RunChannel(string channelID, Action<ChatMessage> messageEvent = null, Action<ChatState> stateEvent = null) => Task.Run(async () =>
        {
            YoutubeChannel channel = await service.GetChannelAsync(channelID);
            if (channel.StreamID == null)
            {
                stateEvent?.Invoke(new ChatState(404, "Stream not founded"));
                return;
            }
            RunVideo(channel.StreamID, messageEvent, stateEvent);
        });
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
                                YoutubeLiveChatInfo liveChatInfo = await service.GetLiveChatInfo(videoID);
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
                            string firstMessageID = null;
                            IEnumerable<ChatMessage> messages = await service.GetChatMessages(dataStream.LiveChat.FullLiveChat);
                            if (messages == null)
                            {
                                dataStream.errors++;
                                continue;
                            }
                            foreach (var message in messages.Where(message => { if (dataStream.lastMessageID == null && message.UtcTime < dataStream.utcInit) return false; if (firstMessageID == null) firstMessageID = message.MessageID; return true; }).TakeWhile(message => dataStream.lastMessageID != message.MessageID).Reverse())
                            {
                                token.ThrowIfCancellationRequested();
                                MessageEvent?.Invoke(videoID, message);
                            }
                            dataStream.errors = 0;
                            dataStream.lastMessageID = firstMessageID ?? dataStream.lastMessageID;
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
        public void Dispose() => canceller.Cancel();
    }
}
