using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Trackr.api {
    /// <summary>
    /// A class for handling instances of MyAnimeList accounts
    /// </summary>
    public class MyAnimeList : Api, IAnime, IManga {
        public new static string Name { get; } = "MyAnimeList";

        private const string UrlBase = "http://myanimelist.net/api/";
        private const string DateFormat = "yyyy-MM-dd";

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
        /// <param name="listStatus">The listStatus to add it under (default is Currently Watching)</param>
        /// <returns>true on success (201), false on failure (400).</returns>
        public async Task<bool> AddAnime(int id, ApiEntry.ListStatuses listStatus = ApiEntry.ListStatuses.Current){
            var data = new FormUrlEncodedContent(new [] {
                new KeyValuePair<string, string>("data,",
                   "<?xml version \"1.0\" encoding=\"UTF-8\"?>" +
                   "<entry>" +
                   "<episode>0</episode>" +
                   "<listStatus>" + listStatus.GetTypeCode() + "</listStatus>" +
                   "</entry>")
            });
            var reponse = await _client.PostAsync(Path.Combine(UrlBase, "animelist", "add", id+".xml"), data);
            return reponse.StatusCode == HttpStatusCode.Created;
        }

        /// <summary>
        /// Remove an anime from the database
        /// </summary>
        /// <param name="id">The anime to remove</param>
        /// <returns>true on success</returns>
        public async Task<bool> RemoveAnime(int id){
            var response = await _client.PostAsync(Path.Combine(UrlBase, "animelist", "delete", id + ".xml"), null);
            return response.Content.ReadAsStringAsync().Result.Contains("Deleted");
        }

        /// <summary>
        /// Search for an anime in the MyAnimeList database
        /// </summary>
        /// <param name="keywords">The search term to use</param>
        /// <returns>A list of all anime found.</returns>
        /// <exception cref="ApiFormatException">if the returned node(s) are malformed.</exception>
        public async Task<List<Anime>> FindAnime(string keywords){
            List<Anime> results = new List<Anime>();
            var response = await _client.PostAsync(Path.Combine(UrlBase, "anime", "search.xml") + "q=" + Uri.EscapeDataString(keywords), null);
            XmlDocument xml = new XmlDocument();
            xml.Load(response.Content.ReadAsStreamAsync().Result);
            XmlNodeList nl = xml.SelectNodes("/anime/entry");
            if (nl != null)
                results.AddRange(from XmlNode n in nl select ToAnime(n));
            return results;
        }

        // convert to anime object from node.
        // This only works for the new API (i.e. does not work for user lists)
        private Anime ToAnime(XmlNode node){
            if(node.Name != "entry") throw new ApiFormatException("The node is not an entry node.");
            if(!node.HasChildNodes) throw new ApiFormatException("The node has no information.");

            try {
                int id = int.Parse(node.SelectSingleNode("/entry/id").Value);
                string title = node.SelectSingleNode("/entry/title").Value;
                string english = node.SelectSingleNode("/entry/english").Value;
                string[] synonyms = Regex.Split(node.SelectSingleNode("/entry/synonyms").Value, "; ");
                int episodes = Int32.Parse(node.SelectSingleNode("/entry/episodes").Value);
                double score = Double.Parse(node.SelectSingleNode("/entry/score").Value);
                Anime.ShowTypes type = ResolveAnimeType(node.SelectSingleNode("/entry/type").Value);
                Anime.RunningStatuses status =
                    ResolveAnimeRunningStatus(node.SelectSingleNode("/entry/status").Value);
                string startstring = node.SelectSingleNode("/entry/start_date").Value;
                string endstring = node.SelectSingleNode("/entry/end_date")?.Value;
                DateTime start = (startstring == "0000-00-00")
                    ? DateTime.MinValue
                    : DateTime.ParseExact(startstring, DateFormat, CultureInfo.InvariantCulture);
                DateTime end = (endstring == "0000-00-00")
                    ? DateTime.MinValue
                    : DateTime.ParseExact(endstring, DateFormat, CultureInfo.InvariantCulture);
                string synopsis = node.SelectSingleNode("/entry/synopsis").Value;
                string url = node.SelectSingleNode("/entry/image").Value;
                return new Anime(id, title, english, synonyms, episodes, score, type, status, start, end, synopsis,
                    url);
            }
            catch (NullReferenceException) {
                throw new ApiFormatException("The node was missing a required value.");
            }
        }

        private Anime.ShowTypes ResolveAnimeType(string type){
            switch (type) {
                case "Special":
                    return Anime.ShowTypes.Special;
                case "Movie":
                    return Anime.ShowTypes.Movie;
                case "OVA":
                    return Anime.ShowTypes.Ova;
                case "ONA":
                    return Anime.ShowTypes.Ona;
                case "Music":
                    return Anime.ShowTypes.Music;
                default:
                    return Anime.ShowTypes.Tv;
            }
        }

        private Anime.RunningStatuses ResolveAnimeRunningStatus(string status){
            switch (status) {
                case "Finished Airing":
                    return Anime.RunningStatuses.Completed;
                case "Currently Airing":
                    return Anime.RunningStatuses.Airing;
                default:
                    return Anime.RunningStatuses.NotYetAired;
            }
        }
    }
}