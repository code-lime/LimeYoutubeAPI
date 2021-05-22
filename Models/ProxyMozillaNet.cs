using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LimeYoutubeAPI.Models
{
    public class ProxyMozillaNet : MozillaNet
    {
        private static readonly Random rnd = new Random((int)(DateTime.Now.Ticks % int.MaxValue));
        private static Uri GetURL(IPEndPoint ip) => new Uri($"http://{ip.Address}:{ip.Port}");

        public List<WebProxy> ProxyList { get; } = new List<WebProxy>();
        private WebProxy RandomProxy => ProxyList[rnd.Next(ProxyList.Count)];
        public ProxyMozillaNet() { }
        public ProxyMozillaNet(Dictionary<IPEndPoint, NetworkCredential> proxyList)
        {
            foreach (var kv in proxyList) ProxyList.Add(new WebProxy(GetURL(kv.Key)) { Credentials = kv.Value });
        }
        public NetworkCredential this[IPEndPoint ip] { set => ProxyList.Add(new WebProxy(GetURL(ip)) { Credentials = value }); }
        protected override HttpClient Client => new HttpClient(new HttpClientHandler { Proxy = RandomProxy });
    }
}
