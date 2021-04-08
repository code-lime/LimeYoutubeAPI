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
                JObject json = await GetYoutubeData(YoutubeURL.GetVideoInfo(videoID));
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

        private IEnumerable<ChatMessage> getChatMessages(JArray chatMessages)
        {
            foreach (JObject item in chatMessages.Reverse())
            {
                ChatMessage msg;
                try
                {
                    if (!item.TryGetValue("addChatItemAction", out JToken chatItem)) continue;
                    chatItem = chatItem["item"]["liveChatTextMessageRenderer"];
                    if (chatItem == null) continue;

                    string message = chatItem["message"]["runs"][0]["text"].Value<string>();
                    string authorName = chatItem["authorName"]?["simpleText"]?.Value<string>() ?? "Unknown";
                    string authorIcon = chatItem["authorPhoto"]?["thumbnails"]?.Last?["url"]?.Value<string>();
                    string authorID = chatItem["authorExternalChannelId"].Value<string>();
                    string messageID = chatItem["id"].Value<string>();
                    long value = chatItem["timestampUsec"].Value<long>() / 1000;
                    DateTime utcTime = DateTimeOffset.FromUnixTimeMilliseconds(value).UtcDateTime;
                    ChannelType authorType = ChannelType.None;
                    foreach (var jbd in chatItem["authorBadges"]?.ToObject<JArray>() ?? new JArray())
                    {
                        JObject jjbd = jbd["liveChatAuthorBadgeRenderer"]?.ToObject<JObject>();
                        if (jjbd != null && jjbd.TryGetValue("icon", out JToken icon))
                        {
                            switch (icon?["iconType"]?.Value<string>())
                            {
                                case "VERIFIED": authorType |= ChannelType.Verified; break;
                                case "MODERATOR": authorType |= ChannelType.Moderator; break;
                                case "OWNER": authorType |= ChannelType.Owner; break;
                                default: authorType |= ChannelType.Other; break;
                            }
                        }
                        else authorType |= ChannelType.Sponsor;
                    }
                    authorIcon = authorIcon?[(authorIcon.LastIndexOf('/') + 1)..authorIcon.IndexOf('=')];
                    msg = new ChatMessage(new ChatChannel(authorID, authorName, authorIcon, authorType), message, utcTime, messageID);
                }
                catch { continue; }
                yield return msg;
            }
        }
        public async Task<IEnumerable<ChatMessage>> GetChatMessages(Uri liveChat)
        {
            JArray chatMessages = await GetChatMessagesData(liveChat);
            return chatMessages == null ? null : getChatMessages(chatMessages);
        }

        private readonly static TimeSpan DefaultUpdate = TimeSpan.FromSeconds(1);

        public ChatListener RunChatListener(string videoID) => RunChatListener(videoID, DefaultUpdate);
        public ChatListener RunChatListener(string videoID, TimeSpan update) => new ChatListener(this, update, videoID);

        public Task<ChatListener> RunChatByChannel(string channelID) => RunChatByChannel(channelID, DefaultUpdate);
        public async Task<ChatListener> RunChatByChannel(string channelID, TimeSpan update)
        {
            YoutubeChannel channel = await GetChannelAsync(channelID);
            if (channel.StreamID == null) return null;
            return RunChatListener(channel.StreamID, update);
        }

        public MultiChatListener RunMultiChatListener() => RunMultiChatListener(DefaultUpdate);
        public MultiChatListener RunMultiChatListener(TimeSpan update) => new MultiChatListener(this, update);

        public void Dispose() => net.Dispose();
    }
}
