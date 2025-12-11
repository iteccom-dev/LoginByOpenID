using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace ClientTest461
{
    public static class SsoHttpClient
    {
        private static readonly Lazy<HttpClient> _client =
            new Lazy<HttpClient>(() =>
            {
                var c = new HttpClient();
                c.BaseAddress = new Uri("https://sso-uat.iteccom.vn/");
                return c;
            });

        public static HttpClient Instance => _client.Value;


    }
}