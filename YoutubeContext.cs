using LimeYoutubeAPI.Base;
using LimeYoutubeAPI.Live;
using LimeYoutubeAPI.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using SpanParser.Json;
using Newtonsoft.Json.Linq;

namespace LimeYoutubeAPI
{
    class YoutubeContext
    {
        PoolArray<char> buffer;
        JsonMemoryContext parseContext;
        YoutubeService service;
        public YoutubeContext(YoutubeService service)
        {
            this.service = service;
            buffer = new PoolArray<char>();
            parseContext = new JsonMemoryContext();
        }

        public void Collect()
        {
            buffer.Clear();
        }
        private async Task<bool> GetNewResponse(Uri youtubeURL)
        {
            var code = await service.GetYoutubeResponse(youtubeURL, buffer);
            return code == System.Net.HttpStatusCode.OK;
        }

        /*private static bool isNew = true;
        private static object _lock = new object();
        private static void WriteTemp(ReadOnlySpan<char> str)
        {
            lock (_lock)
            {
                if (isNew)
                {
                    if (System.IO.File.Exists("temp.json")) System.IO.File.Copy("temp.json", "temp-"+DateTime.Now.Ticks+".json");
                    isNew = false;
                }
                System.IO.File.WriteAllText("temp.json", str.ToString());
            }
        }*/

        internal JSpan GetYoutubeData()
        {
            const string begin = "window[\"ytInitialData\"] = ";
            const string end = ";</script>";
            const string begin2 = "var ytInitialData = ";


            var readed = buffer.Read();

            var text = readed.TakeBetwen(begin, end);
            if (text.IsEmpty)
            {
                text = readed.TakeBetwen(begin2, end);
            }
            //WriteTemp(text);
            var result = JSpan.Parse(text, parseContext);
            return result;
        }

        internal JSpan GetYoutubeInitial()
        {
            const string end = ";var ";
            const string begin = "var ytInitialPlayerResponse = ";

            var text = buffer.Read().TakeBetwen(begin, end);
            var result = JSpan.Parse(text, parseContext);
            var result = JSpan.Parse(text, parseContext);
            return result;
        }

        internal JSpan GetChatMessagesData(JSpan youtubeData)
        {
            if (youtubeData.IsEmpty) return default;

            var tempJson = youtubeData["continuationContents"]["liveChatContinuation"];
            var jtok = (tempJson.IsEmpty ? youtubeData["contents"]["liveChatRenderer"] : tempJson)["actions"];
            if (!jtok.IsArray)
                return default;
            return jtok;
        }

        private YoutubeChannel GetChannel()
        {
            var json = GetYoutubeData();
            string streamID = null;
            var jsonArr = json["contents"]["twoColumnBrowseResultsRenderer"]
                ["tabs"][0]["tabRenderer"]["content"]["sectionListRenderer"]["contents"]
                [0]["itemSectionRenderer"]["contents"][0]["channelFeaturedContentRenderer"]["items"].GetEnumerator();

            while (jsonArr.MoveNext())
            {
                var item = jsonArr.Current;
                if (!item["videoRenderer"]["thumbnailOverlays"][0]["thumbnailOverlayTimeStatusRenderer"]["style"].ToSpan().SequenceEqual("LIVE")) continue;
                streamID = item["videoRenderer"]["videoId"].ToString();
                break;
            }
            json = json["metadata"]["channelMetadataRenderer"];
            ReadOnlySpan<char> icon = json["avatar"]["thumbnails"][0]["url"].ToSpan();
            return new YoutubeChannel(json["externalId"].ToString(), json["title"].ToString(), icon.TakeBetwen('/', '=').ToString(), streamID);
        }
        public async Task<YoutubeChannel> GetChannelAsync(string channelID)
        {
            try
            {
                parseContext.Release();
                var status = await GetNewResponse(YoutubeURL.GetChannel(channelID));
                if (!status) return null;
                var result = GetChannel();
                return result;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private YoutubeVideo GetVideo()
        {
            var json = GetYoutubeInitial();
            if (json.IsEmpty) return null;

            var videoDetails = json["videoDetails"];
            if (videoDetails.IsEmpty) return null;

            var microformat = json["microformat"];

            string VideoID = videoDetails["videoId"].ToString();
            string ChannelID = videoDetails["channelId"].ToString();
            string ChannelName = videoDetails["author"].ToString();

            Uri Preview = new Uri(videoDetails["thumbnail"]["thumbnails"].GetEnumerator().Last()["url"].ToString());

            long Views = long.Parse(videoDetails["viewCount"].ToString());
            string Title = videoDetails["title"].ToString();
            string Description = videoDetails["shortDescription"].ToString();


            var sec = videoDetails["lengthSeconds"];
            TimeSpan VideoLength = TimeSpan.FromSeconds(!sec.IsEmpty ? long.Parse(sec.ToString()) : 0);

            var liveBroadcastDetails = microformat["playerMicroformatRenderer"]["liveBroadcastDetails"];
            bool isLive = !liveBroadcastDetails.IsEmpty;
            var time = liveBroadcastDetails["startTimestamp"];
            if (isLive) return new YoutubeStream(VideoID, ChannelID, ChannelName, Preview, Views, Title, Description, VideoLength, time.IsEmpty ? DateTime.MinValue : time.Deserialize<DateTime>().ToUniversalTime());
            return new YoutubeVideo(VideoID, ChannelID, ChannelName, Preview, Views, Title, Description, VideoLength);
        }
        public async Task<YoutubeVideo> GetVideoAsync(string videoID)
        {
            try
            {
                parseContext.Release();
                var status = await GetNewResponse(YoutubeURL.GetVideo(videoID));
                if (!status) return null;
                var result = GetVideo();
                return result;
            }
            catch(Exception e)
            {
                return null;
            }
        }
        private string GetVideoID()
        {
            var json = GetYoutubeData();
            return json["currentVideoEndpoint"]["watchEndpoint"]["videoId"].ToString();
        }
        public async Task<string> GetVideoIDAsync(Uri video)
        {
            try
            {
                parseContext.Release();
                var status = await GetNewResponse(video);
                if (!status) return null;
                var result = GetVideoID();
                return result;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        private YoutubeLiveChatInfo GetLiveChatInfo()
        {
            var json = GetYoutubeData();
            var tempJson = json["continuationContents"]["liveChatContinuation"];
            var arr = (tempJson.IsEmpty ? json["contents"]["liveChatRenderer"] : tempJson);

            arr = arr["header"]["liveChatHeaderRenderer"]["viewSelector"]["sortFilterSubMenuRenderer"]["subMenuItems"];

            if (!arr.IsArray) return null;
            string idFilter = arr[0]["continuation"]["reloadContinuationData"]["continuation"].ToString();
            string idFull = arr[1]["continuation"]["reloadContinuationData"]["continuation"].ToString();
            return new YoutubeLiveChatInfo(YoutubeURL.GetLiveChatContinuation(idFull), YoutubeURL.GetLiveChatContinuation(idFilter));
        }
        public async Task<YoutubeLiveChatInfo> GetLiveChatInfoAsync(string videoID)
        {
            try
            {
                parseContext.Release();
                var status = await GetNewResponse(YoutubeURL.GetLiveChat(videoID));
                if (!status) return null;
                var result = GetLiveChatInfo();
                return result;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private JSpan GetChatItem(JSpan chatMess, out bool isSponsor)
        {
            isSponsor = false;
            var chatItem = chatMess["addChatItemAction"];
            if (chatItem.IsEmpty) return default;
            var _item = chatItem["item"];
            if (_item.IsEmpty) return default;

            chatItem = _item["liveChatTextMessageRenderer"];
            if (!chatItem.IsEmpty)
            {
                return chatItem;
            }
            else
            {
                chatItem = _item["liveChatMembershipItemRenderer"];
                if (!chatItem.IsEmpty)
                {
                    isSponsor = true;
                    return chatItem;
                }
                else
                {
                    return default;
                }
            }
        }

        private IEnumerable<BaseChatElement> getChatElements(JSpan chatMessages, ref string lastMessId)
        {
            var chatMessagesIter = chatMessages.GetEnumerator();
            chatMessagesIter.ResetToLast();
            var list = new List<BaseChatElement>(chatMessagesIter.Count());
            while (chatMessagesIter.MovePrevious() && GetChatItem(chatMessagesIter.Current, out _).IsEmpty) { }
            var lastChatItem = GetChatItem(chatMessagesIter.Current, out var isSponsor);

            var lastChatItemId = lastChatItem["id"].ToSpan();
            if (lastChatItemId.SequenceEqual(lastMessId)) return list;
            list.Add(isSponsor ? (BaseChatElement)new ChatSponsor(lastChatItem) : new ChatMessage(lastChatItem));
            var newlastMessId = lastChatItemId.ToString();

            while (chatMessagesIter.MovePrevious())
            {
                try
                {
                    var chatItem = GetChatItem(chatMessagesIter.Current, out isSponsor);
                    if (chatItem.IsEmpty) continue;
                    if (chatItem["id"].ToSpan().SequenceEqual(lastMessId)) break;
                    list.Insert(0, isSponsor ? (BaseChatElement)new ChatSponsor(chatItem) : new ChatMessage(chatItem));
                }
                catch { continue; }
            }
            lastMessId = newlastMessId;
            return list;
        }
        public async Task<IEnumerable<BaseChatElement>> GetChatElementsAsync(Uri liveChat)
        {
            try
            {
                parseContext.Release();
                var status = await GetNewResponse(liveChat);
                if (!status) return null;
                string str = null;
                var elm = getChatElements(GetChatMessagesData(GetYoutubeData()), ref str);
                return elm.Any() ? elm : null;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public async Task<(IEnumerable<BaseChatElement> Messages, string NewLastMessageId)> GetNewChatElementsAsync(Uri liveChat, string lastChatMessId)
        {
            try
            {
                parseContext.Release();
                var status = await GetNewResponse(liveChat);
                if (!status) return (null, lastChatMessId);
                var elm = getChatElements(GetChatMessagesData(GetYoutubeData()), ref lastChatMessId);
                return elm.Any() ? (elm, lastChatMessId) : (Enumerable.Empty<BaseChatElement>(), lastChatMessId);
            }
            catch (Exception e)
            {
                return (null, lastChatMessId);
            }
        }
    }
}
