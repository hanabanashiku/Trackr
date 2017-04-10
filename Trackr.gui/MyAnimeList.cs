using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Trackr.gui {
    public class MyAnimeList : Api {
        public new static readonly ApiType Type = ApiType.AnimeManga;
        public new static readonly string Name = "MyAnimeList";

        private const string UrlBase = "http://myanimelist.net/api/";

        private HttpClient client;

        public string Username { get; private set; }
        private ProtectedData password;

        public MyAnimeList(){
            client = new HttpClient();
        }
    }
}