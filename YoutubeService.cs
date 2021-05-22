using LimeYoutubeAPI.Base;
using LimeYoutubeAPI.Interfaces;
using LimeYoutubeAPI.Live;
using LimeYoutubeAPI.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LimeYoutubeAPI
{
    public class YoutubeService : IDisposable
    {
        public static YoutubeService Create(INet net = null) => new YoutubeService(net ?? new MozillaNet());  
        private readonly INet net;
        public YoutubeService(INet net)
        {
            if (net == null) throw new ArgumentNullException("net");
            this.net = net;
        }
        private Task<IResponse> GetYoutubeResponse(Uri youtubeURL) => net.GetAsync(youtubeURL);

        internal async Task<JObject> GetYoutubeData(Uri youtubeURL)
        {
            const string begin = "window[\"ytInitialData\"] = ";
            const string end = ";</script>";
            const string begin2 = "var ytInitialData = ";

            IResponse response = await GetYoutubeResponse(youtubeURL);
            if (response.Code != HttpStatusCode.OK) return null;
            string html = response.Data;
            var indxStart = html.IndexOf(begin);

            if (indxStart == -1)
            {
                indxStart = html.IndexOf(begin2);
                if (indxStart == -1) return JObject.Parse(System.Web.HttpUtility.ParseQueryString(html).Get("player_response"));
                indxStart += begin2.Length;
            }
            else indxStart += begin.Length;

            var indxEnd = html.IndexOf(end, indxStart);
            var text = html[indxStart..indxEnd];
            var result = JObject.Parse(text);
            return result;
        }
        internal async Task<JObject> GetYoutubeInitial(Uri youtubeURL) {
            const string end = ";var ";
            const string begin = "var ytInitialPlayerResponse = ";

            IResponse response = await GetYoutubeResponse(youtubeURL);
            if (response.Code != HttpStatusCode.OK) return null;
            string html = response.Data;
            var indxStart = html.IndexOf(begin) + begin.Length;

            var indxEnd = html.IndexOf(end, indxStart);
            var text = html[indxStart..indxEnd];
            var result = JObject.Parse(text);
            return result;
            //ytInitialPlayerResponse 
        }
        internal async Task<JArray> GetChatMessagesData(Uri liveChatURL)
        {
            JObject json = await GetYoutubeData(liveChatURL);
            if (json == null) return null;
            JToken jtok = (json["continuationContents"]?["liveChatContinuation"] ?? json["contents"]?["liveChatRenderer"])?["actions"];
            if (jtok == null || !(jtok is JArray jarr)) return null;
            return jarr;
        }

        public async Task<YoutubeChannel> GetChannelAsync(string channelID)
        {
            try
            {
                JObject json = await GetYoutubeData(YoutubeURL.GetChannel(channelID));
                string streamID = null;
                foreach (JObject item in json?["contents"]?["twoColumnBrowseResultsRenderer"]?["tabs"]?[0]?["tabRenderer"]?["content"]?["sectionListRenderer"]?["contents"]?[0]?["itemSectionRenderer"]?["contents"]?[0]?["channelFeaturedContentRenderer"]?["items"] ?? new JArray())
                {
                    if (item?["videoRenderer"]?["thumbnailOverlays"]?[0]?["thumbnailOverlayTimeStatusRenderer"]?["style"]?.Value<string>() != "LIVE") continue;
                    streamID = item["videoRenderer"]["videoId"].Value<string>();
                    break;
                }
                json = (JObject)json["metadata"]["channelMetadataRenderer"];
                string icon = json["avatar"]?["thumbnails"]?[0]?["url"]?.Value<string>();
                return new YoutubeChannel(json["externalId"].Value<string>(), json["title"].Value<string>(), icon?[(icon.LastIndexOf('/') + 1)..icon.IndexOf('=')], streamID);
            }
            catch
            {
                return null;
            }
        }
        public async Task<YoutubeVideo> GetVideoAsync(string videoID)
        {
            try
            {
                JObject json = await GetYoutubeInitial(YoutubeURL.GetVideo(videoID));
                if (json == null) return null;
                JObject videoDetails = (JObject)json["videoDetails"];
                if (videoDetails == null) return null;
                JObject microformat = (JObject)json["microformat"];
                string VideoID = videoDetails["videoId"]?.Value<string>();
                string ChannelID = videoDetails["channelId"]?.Value<string>();
                string ChannelName = videoDetails["author"]?.Value<string>();
                Uri Preview = new Uri(videoDetails["thumbnail"]["thumbnails"].Last["url"].Value<string>());
                long Views = long.Parse(videoDetails["viewCount"]?.Value<string>());
                string Title = videoDetails["title"]?.Value<string>();
                string Description = videoDetails["shortDescription"]?.Value<string>();
                TimeSpan VideoLength = TimeSpan.FromSeconds(long.Parse(videoDetails["lengthSeconds"]?.Value<string>() ?? "0"));
                JObject liveBroadcastDetails = (JObject)microformat?["playerMicroformatRenderer"]?["liveBroadcastDetails"];
                bool isLive = liveBroadcastDetails != null;
                if (isLive) return new YoutubeStream(VideoID, ChannelID, ChannelName, Preview, Views, Title, Description, VideoLength, liveBroadcastDetails?["startTimestamp"]?.Value<DateTime>().ToUniversalTime() ?? DateTime.MinValue);
                return new YoutubeVideo(VideoID, ChannelID, ChannelName, Preview, Views, Title, Description, VideoLength);
            }
            catch
            {
                return null;
            }
        }
        public async Task<string> GetVideoIDAsync(Uri video)
        {
            try
            {
                JObject json = await GetYoutubeData(video);
                if (json == null) return null;
                return json?["currentVideoEndpoint"]?["watchEndpoint"]?["videoId"]?.Value<string>();
            }
            catch
            {
                return null;
            }
        }
        public async Task<YoutubeLiveChatInfo> GetLiveChatInfo(string videoID)
        {
            JObject json = await GetYoutubeData(YoutubeURL.GetLiveChat(videoID));
            if (json == null) return null;
            JArray arr = (JArray)(json["continuationContents"]?["liveChatContinuation"] ?? json["contents"]?["liveChatRenderer"])
                ?["header"]
                ?["liveChatHeaderRenderer"]
                ?["viewSelector"]
                ?["sortFilterSubMenuRenderer"]
                ?["subMenuItems"];
            if (arr == null) return null;
            string idFilter = arr[0]["continuation"]?["reloadContinuationData"]?["continuation"]?.Value<string>();
            string idFull = arr[1]["continuation"]?["reloadContinuationData"]?["continuation"]?.Value<string>();
            return new YoutubeLiveChatInfo(YoutubeURL.GetLiveChatContinuation(idFull), YoutubeURL.GetLiveChatContinuation(idFilter));
        }

        private IEnumerable<IChatElement> getChatElements(JArray chatMessages)
        {
            foreach (JObject item in chatMessages.Reverse())
            {
                IChatElement msg;
                try
                {
                    if (!item.TryGetValue("addChatItemAction", out JToken chatItem)) continue;
                    JObject _item = (JObject)chatItem["item"];
                    if (_item == null) continue;
                    if (_item.TryGetValue("liveChatTextMessageRenderer", out chatItem))
                        msg = new ChatMessage(chatItem);
                    else if (_item.TryGetValue("liveChatMembershipItemRenderer", out chatItem))
                        msg = new ChatSponsor(chatItem);
                    else continue;
                }
                catch { continue; }
                yield return msg;
            }
        }
        public async Task<IEnumerable<IChatElement>> GetChatElements(Uri liveChat)
        {
            try
            {
                JArray chatMessages = await GetChatMessagesData(liveChat);
                return chatMessages == null ? null : getChatElements(chatMessages);
            }
            catch
            {
                return null;
            }
        }

        internal readonly static TimeSpan DefaultUpdate = TimeSpan.FromSeconds(1);

        public ChatListener CreateChatListener() => new ChatListener(this);
        public MultiChatListener CreateMultiChatListener() => new MultiChatListener(this);

        public void Dispose() => net.Dispose();
    }
}
