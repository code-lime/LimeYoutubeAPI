using System;
using System.Collections.Generic;
using System.Text;

namespace LimeYoutubeAPI.Interfaces
{
    public interface IResponse
    {
        System.Net.HttpStatusCode Code { get; }
        string Data { get; }
    }
}
