using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LimeYoutubeAPI.Interfaces
{
    public interface INet : IDisposable
    {
        Task<System.Net.HttpStatusCode> GetAsync(Uri url, IBuffer<byte> writeOnlyUTF8Buffer);
        Encoding Encoding { get; }
    }
}
