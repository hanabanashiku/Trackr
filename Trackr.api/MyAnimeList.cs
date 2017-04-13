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
        /// <summary>
        /// The name of the current API.
        /// </summary>
        public new static string Name { get; } = "MyAnimeList";
        /// <summary>
        /// The username of the user logged into the API.
        /// </summary>
        public new string Username { get; }

        private const string UrlBase = "http://myanimelist.net/api/";
        private const string OldUrlBase = "http://myanimelist.net/malappinfo.php?";
        private const string DateFormat = "yyyy-MM-dd";
        private const string DefaultDate = "0000-00-00";

        private readonly HttpClient _client;
        private UserPass _clientLogin;

        /// <summary>
        /// Generate a new MyAnimeList instance
        /// </summary>
        /// <param name="credentials">The encrypted account credentials.</param>
        public MyAnimeList(UserPass credentials){
            _clientLogin = credentials;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _clientLogin.Credentials);
            _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(UserAgent));
            Username = _clientLogin.Username;
        }

        /// <summary>
        /// Verify provided credentials with MyAnimeList.
        /// </summary>
        /// <returns>true if credentials are valid</returns>
        /// <exception cref="ApiRequestException" />
        public async Task<bool> VerifyCredentials(){
            var response = await _client.GetAsync(Path.Combine(UrlBase, "account", "verify_credentials.xml"));

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
            var response = await _client.DeleteAsync(Path.Combine(UrlBase, "animelist", "delete", id + ".xml"));
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
            var response = await _client.GetAsync(Path.Combine(UrlBase, "anime", "search.xml") + "?q=" + Uri.EscapeDataString(keywords));
            XmlDocument xml = new XmlDocument();
            xml.Load(response.Content.ReadAsStreamAsync().Result);
            XmlNodeList nl = xml.SelectNodes("/anime/entry");
            if (nl != null)
                results.AddRange(from XmlNode n in nl select ToAnime(n));
            return results;
        }

        /// <summary>
        /// Update the given entry on the user's remote list.
        /// </summary>
        /// <param name="anime">The anime to update, with updated values.</param>
        /// <returns>true on success</returns>
        public async Task<bool> UpdateAnime(Anime anime){
            if(anime.ListStatus == ApiEntry.ListStatuses.NotInList)
                return RemoveAnime(anime.Id).Result;

            var data = new FormUrlEncodedContent(new [] {
                new KeyValuePair<string, string>("data",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<entry>" +
                        "<episode>" + anime.CurrentEpisode + "</episode>" +
                        "<status>" + (int)anime.ListStatus + "</status>" +
                        "<score>" + anime.UserScore + "</score>" +
                        "<date_start>" + anime.UserStart.ToString(DateFormat) + "</date_start>" +
                        "<date_finish>" + anime.UserEnd.ToString(DateFormat) + "</date_finish>" +
                    "</entry>"),
            });
            var response = await _client.PostAsync(Path.Combine(UrlBase, "animelist", "update", anime.Id + ".xml"),
                data);
            return response.Content.ReadAsStringAsync().Result.Contains("Updated");
        }

        /// <summary>
        /// Get the user's anime list from MyAnimeList.
        /// </summary>
        /// <returns>A list of all anime in the user's list</returns>
        /// <exception cref="ApiFormatException">if the response is malformed.</exception>
        /// <remarks>Note: The old API used for this method does not contain the synopsis, score, or English fields,
        /// so they will be left as String.Empty/0.0 until they are requested by the user. This is not resolved right
        /// away as to limit the number of API calls to MAL.</remarks>
        public async Task<List<Anime>> PullAnimeList(){
            var response = await _client.GetAsync(OldUrlBase + "?u="+Username+"&status=all&type=anime");
            XmlDocument xml = new XmlDocument();
            xml.Load(response.Content.ReadAsStreamAsync().Result);
            List<Anime> results = new List<Anime>();
            if (!xml.FirstChild.HasChildNodes) // returns <myanimelist />
                throw new ApiRequestException("The user's list was not found.");
            XmlNodeList nl = xml.SelectNodes("/myanimelist/anime");
            if (nl != null)
                results.AddRange(from XmlNode n in nl select ToAnimeFromOld(n));
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
                DateTime start = (startstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(startstring, DateFormat, CultureInfo.InvariantCulture);
                DateTime end = (endstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(endstring, DateFormat, CultureInfo.InvariantCulture);
                string synopsis = node.SelectSingleNode("/entry/synopsis").Value;
                string url = node.SelectSingleNode("/entry/image").Value;
                return new Anime(id, title, english, synonyms, episodes, score, type, status, start, end, synopsis,
                    url);
            }
            catch(NullReferenceException) {
                throw new ApiFormatException("The node was missing a required value.");
            }
        }

        // convert a node from an old API call into an Anime object
        private Anime ToAnimeFromOld(XmlNode node){
            if(node.Name != "anime") throw new ApiFormatException("The node is not an anime node");
            if(!node.HasChildNodes) throw new ApiFormatException("The anime node has no information");
            try {
                int id = Int32.Parse(node.SelectSingleNode("/anime/series_animedb_id").Value);
                string title = node.SelectSingleNode("/anime/series_title").Value;
                string[] synonyms = Regex.Split("; ", node.SelectSingleNode("/anime/series_synonyms").Value);
                var type = ResolveAnimeTypeFromOld(node.SelectSingleNode("/anime/series_type").Value);
                int episodes = int.Parse(node.SelectSingleNode("/anime/series_episodes").Value);
                var status = ResolveAnimeRunningStatusFromOld(node.SelectSingleNode("/anime/series_status").Value);
                string startstring = node.SelectSingleNode("/anime/series_start").Value;
                string endstring = node.SelectSingleNode("/anime/series_end").Value;
                DateTime seriesStart = (startstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(startstring, DateFormat, CultureInfo.InvariantCulture);
                DateTime seriesEnd = (startstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(startstring, DateFormat, CultureInfo.InvariantCulture);
                string url = node.SelectSingleNode("/anime/series_image").Value;
                Anime result = new Anime(
                    id, title, string.Empty, synonyms, episodes, 0.0, type, status, seriesStart, seriesEnd,
                    string.Empty, url) {
                    CurrentEpisode = int.Parse(node.SelectSingleNode("/anime/my_watched_episodes").Value)
                };

                // User information
                startstring = node.SelectSingleNode("/anime/my_start_date").Value;
                endstring = node.SelectSingleNode("/anime/my_end_date").Value;
                result.UserStart = (startstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(startstring, DateFormat, CultureInfo.InvariantCulture);
                result.UserEnd = (endstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(endstring, DateFormat, CultureInfo.InvariantCulture);
                result.UserScore = int.Parse(node.SelectSingleNode("/anime/my_score").Value);
                ApiEntry.ListStatuses listStatus;
                Enum.TryParse(node.SelectSingleNode("/anime/my_status").Value, out listStatus);
                result.ListStatus = listStatus;
                return result;
            }
            catch(NullReferenceException) {
                throw new ApiFormatException("The anime node was missing a required value.");
            }
        }
        private static Anime.ShowTypes ResolveAnimeType(string type){
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

        private static Anime.RunningStatuses ResolveAnimeRunningStatus(string status){
            switch (status) {
                case "Finished Airing":
                    return Anime.RunningStatuses.Completed;
                case "Currently Airing":
                    return Anime.RunningStatuses.Airing;
                default:
                    return Anime.RunningStatuses.NotYetAired;
            }
        }

        private static Anime.ShowTypes ResolveAnimeTypeFromOld(string type){
            switch(type) {
                case "2":
                    return Anime.ShowTypes.Ova;
                case "3":
                    return Anime.ShowTypes.Movie;
                case "4":
                    return Anime.ShowTypes.Special;
                case "5":
                    return Anime.ShowTypes.Ona;
                case "6":
                    return Anime.ShowTypes.Music;
                default:
                    return Anime.ShowTypes.Tv;
            }
        }

        private static Anime.RunningStatuses ResolveAnimeRunningStatusFromOld(string status){
            switch(status) {
                case "2":
                    return Anime.RunningStatuses.Completed;
                case "3":
                    return Anime.RunningStatuses.NotYetAired;
                default:
                    return Anime.RunningStatuses.Airing;
            }
        }
    }
}