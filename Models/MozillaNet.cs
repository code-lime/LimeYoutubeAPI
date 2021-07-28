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
        protected virtual HttpWebRequest CreateRequest(Uri url)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.UserAgent = MozillaAgent;
            return request;
        }
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

            //using HttpClient client = Client;
            //client.DefaultRequestHeaders.UserAgent.ParseAdd(MozillaAgent);
            //client.MaxResponseContentBufferSize = 0;


            ////Console.WriteLine("prew reauq");
            ////Console.ReadKey();
            //using HttpResponseMessage response = await client.GetAsync(url);
            //if (response.StatusCode != HttpStatusCode.OK) return response.StatusCode;
            ////Console.WriteLine("after");
            ////Console.ReadKey();
            //var stream = await response.Content.ReadAsStreamAsync();
            //stream.Read(writeOnlyUTF8Buffer.Write((int)stream.Length), 0, (int)stream.Length);
            return HttpStatusCode.OK;
        }

        public virtual void Dispose()
        {


        }
    }
}
