using LimeYoutubeAPI.Base;
using LimeYoutubeAPI.Live;
using LimeYoutubeAPI.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace LimeYoutubeAPI
{
    class YoutubeContext
    {
        PoolArray<char> buffer;
        YoutubeService service;
        public YoutubeContext(YoutubeService service)
        {
            this.service = service;
            buffer = new PoolArray<char>();
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

            //          !!!!!!поправь логику!!!!!

            //var indxStart = html.IndexOf(begin);

            //if (indxStart == -1)
            //{
            //    indxStart = html.IndexOf(begin2);
            //    if (indxStart == -1) return JObject.Parse(System.Web.HttpUtility.ParseQueryString(html).Get("player_response"));
            //    indxStart += begin2.Length;
            //}
            //else indxStart += begin.Length;

            //var indxEnd = html.IndexOf(end, indxStart);

            var text = readed.TakeBetwen(begin, end);
            var result = JSpan.Parse(text);
            return result;
        }

        internal JSpan GetYoutubeInitial()
        {
            const string end = ";var ";
            const string begin = "var ytInitialPlayerResponse = ";

            var text = buffer.Read().TakeBetwen(begin, end);
            var result = JSpan.Parse(text);
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
                if ((ReadOnlySpan<char>)item["videoRenderer"]["thumbnailOverlays"][0]["thumbnailOverlayTimeStatusRenderer"]["style"] != "LIVE") continue;
                streamID = item["videoRenderer"]["videoId"].AsStringValue();
                break;
            }
            json = json["metadata"]["channelMetadataRenderer"];
            ReadOnlySpan<char> icon = json["avatar"]["thumbnails"][0]["url"];
            return new YoutubeChannel(json["externalId"].AsStringValue(), json["title"].AsStringValue(), icon.TakeBetwen('/', '=').ToString(), streamID);
        }
        public async Task<YoutubeChannel> GetChannelAsync(string channelID)
        {
            try
            {
                var status = await GetNewResponse(YoutubeURL.GetChannel(channelID));
                if (!status) return null;
                return GetChannel();
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

            string VideoID = videoDetails["videoId"].AsStringValue();
            string ChannelID = videoDetails["channelId"].AsStringValue();
            string ChannelName = videoDetails["author"].AsStringValue();

            Uri Preview = new Uri(videoDetails["thumbnail"]["thumbnails"].GetEnumerator().GetLast()["url"].AsStringValue());

            long Views = long.Parse(videoDetails["viewCount"].AsStringValue());
            string Title = videoDetails["title"].AsStringValue();
            string Description = videoDetails["shortDescription"].AsStringValue();


            var sec = videoDetails["lengthSeconds"];
            TimeSpan VideoLength = TimeSpan.FromSeconds(!sec.IsEmpty ? long.Parse(sec.AsStringValue()) : 0);

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
                return GetVideo();
            }
            catch(Exception e)
            {
                return null;
            }
        }
        private string GetVideoID()
        {
            var json = GetYoutubeData();
            return json["currentVideoEndpoint"]["watchEndpoint"]["videoId"].AsString();
        }
        public async Task<string> GetVideoIDAsync(Uri video)
        {
            try
            {
                var status = await GetNewResponse(video);
                if (!status) return null;
                return GetVideoID();
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
            var arr = (tempJson.IsEmpty ? json["contents"]["liveChatRenderer"] : tempJson)
                ["header"]
                ["liveChatHeaderRenderer"]
                ["viewSelector"]
                ["sortFilterSubMenuRenderer"]
                ["subMenuItems"];
            if (!arr.IsArray) return null;
            string idFilter = arr[0]["continuation"]["reloadContinuationData"]["continuation"].AsStringValue();
            string idFull = arr[1]["continuation"]["reloadContinuationData"]["continuation"].AsStringValue();
            return new YoutubeLiveChatInfo(YoutubeURL.GetLiveChatContinuation(idFull), YoutubeURL.GetLiveChatContinuation(idFilter));
        }
        public async Task<YoutubeLiveChatInfo> GetLiveChatInfoAsync(string videoID)
        {
            try
            {
                var status = await GetNewResponse(YoutubeURL.GetLiveChat(videoID));
                if (!status) return null;
                return GetLiveChatInfo();
            }
            catch
            {
                return null;
            }
        }

        private IEnumerable<BaseChatElement> getChatElements(JSpan chatMessages)
        {
            var chatMessagesIter = chatMessages.GetReverseEnumerator();
            var list = new List<BaseChatElement>(chatMessagesIter.Count());
            while(chatMessagesIter.MoveNext())
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
                return elm.Any() ? elm : null;
            }
            catch
            {
                return null;
            }
        }

    }
}
