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
using System.Collections.Concurrent;
using System.Threading;

namespace LimeYoutubeAPI
{
    public class YoutubeService : IDisposable
    {
        public static YoutubeService Create(INet net = null) => new YoutubeService(net ?? new MozillaNet());  
        private readonly INet net;

        private PoolArray<byte> receiveBuffer = new PoolArray<byte>();
        private SemaphoreSlim bufferGate = new SemaphoreSlim(1);
        private YoutubeContext context;

        public YoutubeService(INet net)
        {
            if (net == null) throw new ArgumentNullException(nameof(net));
            this.net = net;
            this.context = new YoutubeContext(this);
        }
        internal async Task<HttpStatusCode> GetYoutubeResponse(Uri youtubeURL, PoolArray<char> dataReceiver)
        {
            unsafe void Decode()
            {
                var charCount = net.Encoding.GetCharCount(receiveBuffer.Read());
                net.Encoding.GetChars(receiveBuffer.ReadByPointer(), receiveBuffer.Length, dataReceiver.WriteByPointer(charCount), charCount);
            }

            await bufferGate.WaitAsync();
            receiveBuffer.Release();
            var code = await net.GetAsync(youtubeURL, receiveBuffer);
            if (code != HttpStatusCode.OK)
            {
                bufferGate.Release();
                return code;
            }

            Decode();
            bufferGate.Release();
            return HttpStatusCode.OK;
        }

        //internal JSpan GetYoutubeData(Uri youtubeURL, object sender)
        //{
        //    const string begin = "window[\"ytInitialData\"] = ";
        //    const string end = ";</script>";
        //    const string begin2 = "var ytInitialData = ";

        //    if (GetYoutubeResponse(youtubeURL, sender, out var data) != HttpStatusCode.OK) return default;

        //    var readed = data.Read();

        //    //          !!!!!!поправь логику!!!!!

        //    //var indxStart = html.IndexOf(begin);

        //    //if (indxStart == -1)
        //    //{
        //    //    indxStart = html.IndexOf(begin2);
        //    //    if (indxStart == -1) return JObject.Parse(System.Web.HttpUtility.ParseQueryString(html).Get("player_response"));
        //    //    indxStart += begin2.Length;
        //    //}
        //    //else indxStart += begin.Length;

        //    //var indxEnd = html.IndexOf(end, indxStart);

        //    var text = readed.TakeBetwen(begin, end);
        //    var result = JSpan.Parse(text);
        //    return result;
        //}

        //internal JSpan GetYoutubeInitial(Uri youtubeURL, object sender) {
        //    const string end = ";var ";
        //    const string begin = "var ytInitialPlayerResponse = ";

        //    if (GetYoutubeResponse(youtubeURL, sender, out var data) != HttpStatusCode.OK) return default;

        //    var text = data.Read().TakeBetwen(begin, end);
        //    var result = JSpan.Parse(text);
        //    return result;
        //}

        //internal JSpan GetChatMessagesData(Uri liveChatURL, object sender)
        //{
        //    var json = GetYoutubeData(liveChatURL, sender);
        //    if (json.IsEmpty) return default;

        //    var tempJson = json["continuationContents"]["liveChatContinuation"];
        //    var jtok = (tempJson.IsEmpty ? json["contents"]["liveChatRenderer"] : tempJson)["actions"];
        //    if (!jtok.IsArray) return default;
        //    return jtok;
        //}

        //public async Task<YoutubeChannel> GetChannelAsync(string channelID)
        //{
        //    try
        //    {
        //        JObject json = await GetYoutubeData(YoutubeURL.GetChannel(channelID));
        //        string streamID = null;
        //        foreach (JObject item in json?["contents"]?["twoColumnBrowseResultsRenderer"]?["tabs"]?[0]?["tabRenderer"]?["content"]?["sectionListRenderer"]?["contents"]?[0]?["itemSectionRenderer"]?["contents"]?[0]?["channelFeaturedContentRenderer"]?["items"] ?? new JArray())
        //        {
        //            if (item?["videoRenderer"]?["thumbnailOverlays"]?[0]?["thumbnailOverlayTimeStatusRenderer"]?["style"]?.Value<string>() != "LIVE") continue;
        //            streamID = item["videoRenderer"]["videoId"].Value<string>();
        //            break;
        //        }
        //        json = (JObject)json["metadata"]["channelMetadataRenderer"];
        //        string icon = json["avatar"]?["thumbnails"]?[0]?["url"]?.Value<string>();
        //        return new YoutubeChannel(json["externalId"].Value<string>(), json["title"].Value<string>(), icon?[(icon.LastIndexOf('/') + 1)..icon.IndexOf('=')], streamID);
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        //public async Task<YoutubeVideo> GetVideoAsync(string videoID)
        //{
        //    try
        //    {
        //        JObject json = await GetYoutubeInitial(YoutubeURL.GetVideo(videoID));
        //        if (json == null) return null;
        //        JObject videoDetails = (JObject)json["videoDetails"];
        //        if (videoDetails == null) return null;
        //        JObject microformat = (JObject)json["microformat"];
        //        string VideoID = videoDetails["videoId"]?.Value<string>();
        //        string ChannelID = videoDetails["channelId"]?.Value<string>();
        //        string ChannelName = videoDetails["author"]?.Value<string>();
        //        Uri Preview = new Uri(videoDetails["thumbnail"]["thumbnails"].Last["url"].Value<string>());
        //        long Views = long.Parse(videoDetails["viewCount"]?.Value<string>());
        //        string Title = videoDetails["title"]?.Value<string>();
        //        string Description = videoDetails["shortDescription"]?.Value<string>();
        //        TimeSpan VideoLength = TimeSpan.FromSeconds(long.Parse(videoDetails["lengthSeconds"]?.Value<string>() ?? "0"));
        //        JObject liveBroadcastDetails = (JObject)microformat?["playerMicroformatRenderer"]?["liveBroadcastDetails"];
        //        bool isLive = liveBroadcastDetails != null;
        //        if (isLive) return new YoutubeStream(VideoID, ChannelID, ChannelName, Preview, Views, Title, Description, VideoLength, liveBroadcastDetails?["startTimestamp"]?.Value<DateTime>().ToUniversalTime() ?? DateTime.MinValue);
        //        return new YoutubeVideo(VideoID, ChannelID, ChannelName, Preview, Views, Title, Description, VideoLength);
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
        //public async Task<string> GetVideoIDAsync(Uri video)
        //{
        //    try
        //    {
        //        JObject json = await GetYoutubeData(video);
        //        if (json == null) return null;
        //        return json?["currentVideoEndpoint"]?["watchEndpoint"]?["videoId"]?.Value<string>();
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
        //public async Task<YoutubeLiveChatInfo> GetLiveChatInfo(string videoID)
        //{
        //    JObject json = await GetYoutubeData(YoutubeURL.GetLiveChat(videoID));
        //    if (json == null) return null;
        //    JArray arr = (JArray)(json["continuationContents"]?["liveChatContinuation"] ?? json["contents"]?["liveChatRenderer"])
        //        ?["header"]
        //        ?["liveChatHeaderRenderer"]
        //        ?["viewSelector"]
        //        ?["sortFilterSubMenuRenderer"]
        //        ?["subMenuItems"];
        //    if (arr == null) return null;
        //    string idFilter = arr[0]["continuation"]?["reloadContinuationData"]?["continuation"]?.Value<string>();
        //    string idFull = arr[1]["continuation"]?["reloadContinuationData"]?["continuation"]?.Value<string>();
        //    return new YoutubeLiveChatInfo(YoutubeURL.GetLiveChatContinuation(idFull), YoutubeURL.GetLiveChatContinuation(idFilter));
        //}

        //private IEnumerable<BaseChatElement> getChatElements(JArray chatMessages)
        //{
        //    foreach (JObject item in chatMessages.Reverse())
        //    {
        //        BaseChatElement msg;
        //        try
        //        {
        //            if (!item.TryGetValue("addChatItemAction", out JToken chatItem)) continue;
        //            JObject _item = (JObject)chatItem["item"];
        //            if (_item == null) continue;
        //            if (_item.TryGetValue("liveChatTextMessageRenderer", out chatItem))
        //                msg = new ChatMessage(chatItem);
        //            else if (_item.TryGetValue("liveChatMembershipItemRenderer", out chatItem))
        //                msg = new ChatSponsor(chatItem);
        //            else continue;
        //        }
        //        catch { continue; }
        //        yield return msg;
        //    }
        //}
        //public async Task<IEnumerable<BaseChatElement>> GetChatElements(Uri liveChat)
        //{
        //    try
        //    {
        //        JArray chatMessages = await GetChatMessagesData(liveChat);
        //        return chatMessages == null ? null : getChatElements(chatMessages);
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        internal readonly static TimeSpan DefaultUpdate = TimeSpan.FromSeconds(1);

        public ChatListener CreateChatListener() => new ChatListener(new YoutubeContext(this));
        public MultiChatListener CreateMultiChatListener() => new MultiChatListener(new YoutubeContext(this));

        private SemaphoreSlim apiSlim = new SemaphoreSlim(1);
        public async Task<YoutubeChannel> GetChannelAsync(string channelID)
        {
            await apiSlim.WaitAsync();
            YoutubeChannel channel = await this.context.GetChannelAsync(channelID);
            apiSlim.Release();
            return channel;
        }
        public async Task<YoutubeVideo> GetVideoAsync(string videoID)
        {
            await apiSlim.WaitAsync();
            YoutubeVideo video = await this.context.GetVideoAsync(videoID);
            apiSlim.Release();
            return video;
        }
        public async Task<string> GetVideoIDAsync(Uri video)
        {
            await apiSlim.WaitAsync();
            string data = await this.context.GetVideoIDAsync(video);
            apiSlim.Release();
            return data;
        }
        public async Task<YoutubeLiveChatInfo> GetLiveChatInfoAsync(string videoID)
        {
            await apiSlim.WaitAsync();
            YoutubeLiveChatInfo youtube = await this.context.GetLiveChatInfoAsync(videoID);
            apiSlim.Release();
            return youtube;
        }

        public void Dispose() => net.Dispose();
    }
}
