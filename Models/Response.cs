using LimeYoutubeAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace LimeYoutubeAPI.Models
{
    public class Response : IResponse
    {
        public HttpStatusCode Code { get; }
        public string Data { get; }

        public Response(System.Net.HttpStatusCode code, string data)
        {
            Code = code;
            Data = data;
        }
    }
}
