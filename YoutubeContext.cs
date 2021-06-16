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
            var result = JSpan.Parse(text, parseContext);
            return result;
        }

        internal JSpan GetYoutubeInitial()
        {
            const string end = ";var ";
            const string begin = "var ytInitialPlayerResponse = ";

            var text = buffer.Read().TakeBetwen(begin, end);
            var result = JSpan.Parse(text, parseContext);
            return result;
        }

        internal JSpan GetChatMessagesData(JSpan youtubeData)
        {
            if (youtubeData.IsEmpty) return default;

            var tempJson = youtubeData["continuationContents"]["liveChatContinuation"];
            var jtok = (tempJson.IsEmpty ? youtubeData["contents"]["liveChatRenderer"] : tempJson)["actions"];
            if (!jtok.IsArray) return default;
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
                if (item["videoRenderer"]["thumbnailOverlays"][0]["thumbnailOverlayTimeStatusRenderer"]["style"].ToSpan() != "LIVE") continue;
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
                var status = await GetNewResponse(YoutubeURL.GetChannel(channelID));
                if (!status) return null;
                var result = GetChannel();
                parseContext.Release();
                return result;
            }
            catch
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
                var status = await GetNewResponse(YoutubeURL.GetVideo(videoID));
                if (!status) return null;
                var result = GetVideo();
                parseContext.Release();
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
                var status = await GetNewResponse(video);
                if (!status) return null;
                var result = GetVideoID();
                parseContext.Release();
                return result;
            }
            catch
            {
                return null;
            }
        }
        private YoutubeLiveChatInfo GetLiveChatInfo()
        {
            var json = GetYoutubeData();
            var tempJson = json["continuationContents"]["liveChatContinuation"];
            var arr = (tempJson.IsEmpty ? json["contents"]["liveChatRenderer"] : tempJson);

            arr = arr["header"];
            tempJson = arr["liveChatHeaderRenderer"];
            arr = tempJson.IsEmpty ? arr["liveChatBannerHeaderRenderer"] : tempJson;
            arr = arr["viewSelector"];
            arr = arr["sortFilterSubMenuRenderer"];
            arr = arr["subMenuItems"];

            if (!arr.IsArray) return null;
            string idFilter = arr[0]["continuation"]["reloadContinuationData"]["continuation"].ToString();
            string idFull = arr[1]["continuation"]["reloadContinuationData"]["continuation"].ToString();
            return new YoutubeLiveChatInfo(YoutubeURL.GetLiveChatContinuation(idFull), YoutubeURL.GetLiveChatContinuation(idFilter));
        }
        public async Task<YoutubeLiveChatInfo> GetLiveChatInfoAsync(string videoID)
        {
            try
            {
                var status = await GetNewResponse(YoutubeURL.GetLiveChat(videoID));
                if (!status) return null;
                var result = GetLiveChatInfo();
                parseContext.Release();
                return result;
            }
            catch
            {
                return null;
            }
        }

        private IEnumerable<BaseChatElement> getChatElements(JSpan chatMessages)
        {
            var chatMessagesIter = chatMessages.GetEnumerator();
            chatMessagesIter.ResetToLast();
            var list = new List<BaseChatElement>(chatMessagesIter.Count());
            while(chatMessagesIter.MovePrevious())
            {
                BaseChatElement msg;
                try
                {
                    var chatItem = chatMessagesIter.Current["addChatItemAction"];
                    if (chatItem.IsEmpty) continue;
                    var _item = chatItem["item"];
                    if (_item.IsEmpty) continue;

                    chatItem = _item["liveChatTextMessageRenderer"];
                    if (!chatItem.IsEmpty)
                    {
                        msg = new ChatMessage(chatItem);
                    }
                    else
                    {
                        chatItem = _item["liveChatMembershipItemRenderer"];
                        if (!chatItem.IsEmpty)
                        {
                            msg = new ChatSponsor(chatItem);
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                catch { continue; }
                list.Add(msg);
            }
            return list;
        }
        public async Task<IEnumerable<BaseChatElement>> GetChatElementsAsync(Uri liveChat)
        {
            try
            {
                var status = await GetNewResponse(liveChat);
                if (!status) return null;
                var elm = getChatElements(GetChatMessagesData(GetYoutubeData()));
                parseContext.Release();
                return elm.Any() ? elm : null;
            }
            catch
            {
                return null;
            }
        }

    }
}
