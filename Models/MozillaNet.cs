using LimeYoutubeAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
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

        public Encoding Encoding => Encoding.UTF8;

        public virtual async Task<HttpStatusCode> GetAsync(Uri url, IBuffer<byte> writeOnlyUTF8Buffer)
        {

            using HttpClient client = Client;
            client.DefaultRequestHeaders.UserAgent.ParseAdd(MozillaAgent);
            using HttpResponseMessage response = await client.GetAsync(url);
            if (response.StatusCode != HttpStatusCode.OK) return response.StatusCode;

            var stream = await response.Content.ReadAsStreamAsync();

            stream.Read(writeOnlyUTF8Buffer.Write((int)stream.Length), 0, (int)stream.Length);

            return HttpStatusCode.OK;
        }

        public virtual void Dispose() { }
    }
}
