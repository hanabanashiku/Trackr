using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml;

namespace Trackr.api {
    /// <summary>
    /// A class for handling instances of MyAnimeList accounts
    /// </summary>
    public class MyAnimeList : Api, IAnime, IManga {
        public new static string Name { get; } = "MyAnimeList";

        private const string UrlBase = "http://myanimelist.net/api/";

        private readonly HttpClient _client;
        private UserPass _clientLogin;

        /// <summary>
        /// Generate a new MyAnimeList instance
        /// </summary>
        /// <param name="username">The account username</param>
        /// <param name="password">The account password</param>
        public MyAnimeList(string username, string password){
            _clientLogin = new UserPass(username, password);
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _clientLogin.Credentials);
            _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(UserAgent));
        }

        /// <summary>
        /// Verify provided credentials with MyAnimeList.
        /// </summary>
        /// <returns>true if credentials are valid</returns>
        /// <exception cref="ApiRequestException" />
        public async Task<bool> VerifyCredentials(){
            var data = new FormUrlEncodedContent(new [] {
                new KeyValuePair<string, string>("data", "XML"),
            });
            var response = await _client.PostAsync(Path.Combine(UrlBase, "account", "verify_credentials.xml"), data);

            var xml = new XmlDocument();
                xml.Load(response.Content.ReadAsStreamAsync().Result);
            var tag = xml.GetElementsByTagName("user");
            if(tag.Count == 0)
                throw new ApiRequestException("MAL credential response not understood.");
            return tag.Item(0)?.FirstChild.Value == "1";
        }

        /// <summary>
        /// Add an anime to the list
        /// </summary>
        /// <param name="id">The MAL ID number of the given anime</param>
        /// <param name="status">The status to add it under (default is Currently Watching)</param>
        /// <returns>true on success (201), false on failure (400).</returns>
        public async Task<bool> AddAnime(int id, ApiEntry.Status status = ApiEntry.Status.Current){
            var data = new FormUrlEncodedContent(new [] {
                new KeyValuePair<string, string>("data,",
                   "<?xml version \"1.0\" encoding=\"UTF-8\"?>" +
                   "<entry>" +
                   "<episode>0</episode>" +
                   "<status>" + status.GetTypeCode() + "<status>" +
                   "</entry>")
            });
            var reponse = await _client.PostAsync(Path.Combine(UrlBase, "animelist", "add", id+".xml"), data);
            return reponse.StatusCode == HttpStatusCode.Created;
        }
    }
}