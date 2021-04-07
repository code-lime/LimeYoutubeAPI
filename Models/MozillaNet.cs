using LimeYoutubeAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LimeYoutubeAPI.Models
{
    public class MozillaNet : INet
    {
        public virtual string MozillaAgent => "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36";
        protected virtual HttpClient Client => new HttpClient();
        public virtual async Task<IResponse> GetAsync(Uri url)
        {
            using HttpClient client = Client;
            client.DefaultRequestHeaders.UserAgent.ParseAdd(MozillaAgent);
            using HttpResponseMessage response = await client.GetAsync(url);
            if (response.StatusCode != System.Net.HttpStatusCode.OK) return new Response(response.StatusCode, null);
            string html = await response.Content.ReadAsStringAsync();
            return new Response(response.StatusCode, html);
        }
        public virtual void Dispose() { }
    }
}
