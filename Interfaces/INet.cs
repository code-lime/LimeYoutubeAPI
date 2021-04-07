using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LimeYoutubeAPI.Interfaces
{
    public interface INet : IDisposable
    {
        Task<IResponse> GetAsync(Uri url);
    }
}
