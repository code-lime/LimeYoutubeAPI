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
        protected virtual HttpWebRequest CreateRequest(Uri url) => WebRequest.CreateHttp(url);

        public Encoding Encoding => Encoding.UTF8;

        private byte[] copyBuffer = new byte[8_192];
        public virtual async Task<HttpStatusCode> GetAsync(Uri url, IBuffer<byte> writeOnlyUTF8Buffer)
        {
            var request = CreateRequest(url);
            var response = (HttpWebResponse)await request.GetResponseAsync();
            if (response.StatusCode != HttpStatusCode.OK) return response.StatusCode;

            using (var stream = response.GetResponseStream())
            {
                int len;
                do
                {
                    len = stream.Read(copyBuffer, 0, copyBuffer.Length);
                    writeOnlyUTF8Buffer.WriteBuffer(copyBuffer, len);
                }
                while (len > 0);
            }

            return HttpStatusCode.OK;
        }

        public virtual void Dispose() { }
    }
}
