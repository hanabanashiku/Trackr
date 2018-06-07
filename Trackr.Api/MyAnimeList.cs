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
using Trackr.Core;
// ReSharper disable PossibleNullReferenceException

namespace Trackr.Api {
    /// <summary>
    /// A class for handling instances of MyAnimeList accounts
    /// </summary>
    // TODO: Add Japanese title support
    [Serializable]
    public class MyAnimeList : Api, IAnime, IManga {
		public const string Identifier = "MyAnimeList";
        /// <summary>
        /// The name of the current API.
        /// </summary>
        public override string Name { get; } = Identifier;
        /// <summary>
        /// The username of the user logged into the API.
        /// </summary>
        public override string Username => _clientLogin.Username;

        private const string UrlBase = "https://myanimelist.net/api/";
        private const string OldUrlBase = "https://myanimelist.net/malappinfo.php";
        private const string DateFormat = "yyyy-MM-dd";
        private const string DateRequestFormat = "MMddyyyy";
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
            //_client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        }

        /// <summary>
        /// Verify provided credentials with MyAnimeList.
        /// </summary>
        /// <returns>true if credentials are valid</returns>
        /// <exception cref="ApiFormatException">if the request times out.</exception>
        /// <exception cref="WebException">if a connection cannot be established.</exception>
        public override async Task<bool> VerifyCredentials(){
            var response = await _client.GetAsync(Path.Combine(UrlBase, "account", "verify_credentials.xml"));
            Console.WriteLine(response.StatusCode);
            if(response.StatusCode == HttpStatusCode.RequestTimeout)
                throw new ApiRequestException("The request timed out.");
            if(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden)
                throw new ApiRequestException("The requested service is temporarily unavailable.");
            return response.StatusCode == HttpStatusCode.OK; // If credentials are wrong it throws a 401 error
        }

        /// <summary>
        /// Add an anime to the authenticated user's list.
        /// </summary>
        /// <param name="id">The MAL ID number of the given anime</param>
        /// <param name="listStatus">The listStatus to add it under (default is Currently Watching)</param>
        /// <returns>true on success (201), false on failure (400).</returns>
        /// <exception cref="ArgumentException">If the anime list status is set to be "not in list".</exception>
        /// <exception cref="ApiFormatException">if the request times out.</exception>
        /// <exception cref="WebException">if a connection cannot be established.</exception>
        public async Task<bool> AddAnime(int id, ApiEntry.ListStatuses listStatus = ApiEntry.ListStatuses.Current){
            if(listStatus == ApiEntry.ListStatuses.NotInList) throw new ArgumentException("Cannot add a list item that is set to not be in the list");
            var data = new FormUrlEncodedContent(new [] {
                new KeyValuePair<string, string>("data",
                   "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                   "<entry>" +
                   "<episode>0</episode>" +
                   "<status>" + (int)listStatus + "</status>" +
                   "</entry>")
            });
            var response = await _client.PostAsync(Path.Combine(UrlBase, "animelist", "add", id+".xml"), data);
            if(response.StatusCode == HttpStatusCode.RequestTimeout)
                throw new ApiRequestException("The request timed out.");
            return response.StatusCode == HttpStatusCode.Created;
        }

        /// <summary>
        /// Remove an anime from the authenticated user's list.
        /// </summary>
        /// <param name="id">The MAL ID of the anime to remove</param>
        /// <returns>true on success</returns>
        /// <exception cref="ApiFormatException">if the request times out.</exception>
        /// <exception cref="WebException">if a connection cannot be established.</exception>
        public async Task<bool> RemoveAnime(int id){
            var response = await _client.DeleteAsync(Path.Combine(UrlBase, "animelist", "delete", id + ".xml"));
            if(response.StatusCode == HttpStatusCode.RequestTimeout)
                throw new ApiRequestException("The request timed out.");
            return response.Content.ReadAsStringAsync().Result == "Deleted";
        }

        /// <summary>
        /// Search for an anime in the MyAnimeList database.
        /// </summary>
        /// <param name="keywords">The search term to use</param>
        /// <returns>A list of all anime found.</returns>
        /// <exception cref="ApiFormatException">if the request times out.</exception>
        /// <exception cref="WebException">if a connection cannot be established.</exception>
        public async Task<List<Anime>> FindAnime(string keywords){
            var results = new List<Anime>();
            var response = await _client.GetAsync(Path.Combine(UrlBase, "anime", "search.xml") + "?q=" + Uri.EscapeDataString(keywords));
            if(response.StatusCode == HttpStatusCode.RequestTimeout)
                throw new ApiRequestException("The request timed out.");
            if(response.StatusCode == HttpStatusCode.NoContent)
                return new List<Anime>();
            var xml = new XmlDocument();
            xml.Load(response.Content.ReadAsStreamAsync().Result);
            var nl = xml.SelectNodes("/anime/entry");
            if (nl != null)
                results.AddRange(from XmlNode n in nl select ToAnime(n));
            return results;
        }

        /// <summary>
        /// Update the given entry on the authenticated user's list.
        /// </summary>
        /// <param name="anime">The anime to update, with updated values.</param>
        /// <returns>true on success</returns>
        /// <exception cref="ApiFormatException">if the request times out.</exception>
        /// <exception cref="WebException">if a connection cannot be established.</exception>
        public async Task<bool> UpdateAnime(Anime anime){
            if(anime.ListStatus == ApiEntry.ListStatuses.NotInList)
                return RemoveAnime(anime.Id).Result;

            if(anime.ListStatus == ApiEntry.ListStatuses.Completed)
                anime.CurrentEpisode = anime.Episodes;

            string start = (anime.UserStart == DateTime.MinValue) ? DefaultDate : anime.UserStart.ToString(DateRequestFormat);
            string end = (anime.UserEnd == DateTime.MinValue) ? DefaultDate : anime.UserEnd.ToString(DateRequestFormat);

            var data = new FormUrlEncodedContent(new [] {
                new KeyValuePair<string, string>("data",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<entry>" +
                        "<episode>" + anime.CurrentEpisode + "</episode>" +
                        "<status>" + (int)anime.ListStatus + "</status>" +
                        "<score>" + anime.UserScore + "</score>" +
                        "<date_start>" + start + "</date_start>" +
                        "<date_finish>" + end + "</date_finish>" +
                    "</entry>"),
            });
            var response = await _client.PostAsync(Path.Combine(UrlBase, "animelist", "update", anime.Id + ".xml"),
                data);
            if(response.StatusCode == HttpStatusCode.RequestTimeout)
                throw new ApiRequestException("The request timed out.");
            return response.Content.ReadAsStringAsync().Result.Contains("Updated");
        }

        /// <summary>
        /// Get the authenticated user's anime list.
        /// </summary>
        /// <returns>A list of all anime in the user's list from the server.</returns>
        /// <exception cref="ApiFormatException">if the request times out.</exception>
        /// <exception cref="WebException">if a connection cannot be established.</exception>
        /// <remarks>Note: The old API used for this method does not contain the synopsis, score, or English fields,
        /// so they will be left as String.Empty/0.0 until they are requested by the user. This is not resolved right
        /// away as to limit the number of API calls to MAL.</remarks>
        public async Task<List<Anime>> PullAnimeList(){
            var response = await _client.GetAsync(OldUrlBase + "?u="+Username+"&status=all&type=anime");
            if(response.StatusCode == HttpStatusCode.RequestTimeout)
                throw new ApiRequestException("The request timed out.");
            XmlDocument xml = new XmlDocument();
            xml.Load(response.Content.ReadAsStreamAsync().Result);
            List<Anime> results = new List<Anime>();
            XmlNode root = xml.SelectSingleNode("/myanimelist");
            if (root == null || !root.HasChildNodes) // returns <myanimelist />
                throw new ApiRequestException("The user's list was not found.");
            XmlNodeList nl = xml.SelectNodes("/myanimelist/anime");
            results.AddRange(from XmlNode n in nl select ToAnimeFromOld(n));
            return results;
        }

        /// <summary>
        /// Add a manga to the authenticated user's list.
        /// </summary>
        /// <param name="id">The MAL ID of the given manga.</param>
        /// <param name="listStatus">The list status of the manga (default is currently reading).</param>
        /// <returns>true on success</returns>
        /// <exception cref="ApiFormatException">if the request times out.</exception>
        /// <exception cref="WebException">if a connection cannot be established.</exception>
        public async Task<bool> AddManga(int id, ApiEntry.ListStatuses listStatus = ApiEntry.ListStatuses.Current) {
            var data = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("data",
                "<?xml version =\"1.0\" encoding=\"UTF-8\"?>" + 
                "<entry>" + 
                    "<chapter>0</chapter>" + 
                    "<volume>0</volume>" + 
                    "<status>" + (int)listStatus + "</status>" + 
                "</entry>")
            });
            var response = await _client.PostAsync(Path.Combine(UrlBase, "mangalist", "add", id + ".xml"), data);
            if(response.StatusCode == HttpStatusCode.RequestTimeout)
                throw new ApiRequestException("The request timed out.");
            return response.StatusCode == HttpStatusCode.Created;
        }

        /// <summary>
        /// Remove a manga from the authenticated user's list.
        /// </summary>
        /// <param name="id">The MAL ID of the manga to remove.</param>
        /// <returns>true on success</returns>
        /// <exception cref="ApiFormatException">if the request times out.</exception>
        /// <exception cref="WebException">if a connection cannot be established.</exception>
        public async Task<bool> RemoveManga(int id) {
            var response = await _client.DeleteAsync(Path.Combine(UrlBase, "mangalist", "delete", id + ".xml"));
            return response.Content.ReadAsStringAsync().Result == "Deleted";
        }

        /// <summary>
        /// Search for a manga in the MyAnimeList database.
        /// </summary>
        /// <param name="keywords">The search terms to use.</param>
        /// <returns>A list of all relevant results.</returns>
        /// <exception cref="ApiFormatException">If the response was malformed.</exception>
        public async Task<List<Manga>> FindManga(string keywords){
            List<Manga> result = new List<Manga>();
            var response = await _client.GetAsync(Path.Combine(UrlBase, "manga", "search.xml?q=") + keywords);
            if(response.StatusCode == HttpStatusCode.NoContent)
                return result;
            XmlDocument xml = new XmlDocument();
            xml.Load(response.Content.ReadAsStreamAsync().Result);
            XmlNodeList nl = xml.SelectNodes("/manga/entry");
            result.AddRange(from XmlNode n in nl select ToManga(n));
            return result;
        }

        /// <summary>
        /// Upedate the given entry on the authenticated user's list.
        /// </summary>
        /// <param name="manga">The manga to update with updated list values</param>
        /// <returns>true on success.</returns>
        /// <exception cref="ApiFormatException">if the request times out.</exception>
        /// <exception cref="WebException">if a connection cannot be established.</exception>
        public async Task<bool> UpdateManga(Manga manga){
            Console.WriteLine(manga.Title + " " + manga.ListStatus);
            if(manga.ListStatus == ApiEntry.ListStatuses.NotInList)
                return RemoveManga(manga.Id).Result;
            if(manga.ListStatus == ApiEntry.ListStatuses.Completed) {
                manga.CurrentChapter = manga.Chapters;
            }

            string start = (manga.UserStart == DateTime.MinValue) ? DefaultDate : manga.UserStart.ToString(DateRequestFormat);
            string end = (manga.UserEnd == DateTime.MinValue) ? DefaultDate : manga.UserEnd.ToString(DateRequestFormat);
            var data = new FormUrlEncodedContent(new [] {
                new KeyValuePair<string, string>("data",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<entry>" +
                        "<chapter>" + manga.CurrentChapter + "</chapter>" +
                        "<volume>" + manga.CurrentVolume + "</volume>" +
                        "<status>" + (int)manga.ListStatus + "</status>" +
                        "<score>" + manga.UserScore + "</score>" +
                        "<date_start>" + start + "</date_start>" +
                        "<date_finish>" + end + "</date_finish>" +
                    "</entry>"),
            });
            var response = await _client.PostAsync(Path.Combine(UrlBase, "mangalist", "update", manga.Id + ".xml"), data);
            Console.WriteLine(response.StatusCode + " " + response.Content.ReadAsStringAsync().Result);
            return response.Content.ReadAsStringAsync().Result.Contains("Updated");
        }

        /// <summary>
        /// Get the authenticated user's manga list.
        /// </summary>
        /// <returns>A list of all manga in the user's list</returns>
        /// <exception cref="ApiFormatException">if the request times out.</exception>
        /// <exception cref="WebException">if a connection cannot be established.</exception>
        /// <remarks>Note: The old API used for this method does not contain the synopsis, score, or English fields,
        /// so they will be left as String.Empty/0.0 until they are requested by the user. This is not resolved right
        /// away as to limit the number of API calls to MAL.</remarks>
        public async Task<List<Manga>> PullMangaList(){
            var response = await _client.GetAsync(OldUrlBase + "?u="+Username+"&status=all&type=manga");
            XmlDocument xml = new XmlDocument();
            xml.Load(response.Content.ReadAsStreamAsync().Result);
            List<Manga> results = new List<Manga>();
            XmlNode root = xml.SelectSingleNode("/myanimelist");
            if (root == null || !root.HasChildNodes) // returns <myanimelist />
                throw new ApiRequestException("The user's list was not found.");
            XmlNodeList nl = xml.SelectNodes("/myanimelist/manga");
            if (nl != null)
                results.AddRange(from XmlNode n in nl select ToMangaFromOld(n));
            return results;
        }

        // convert to anime object from node.
        // This only works for the new API (i.e. does not work for user lists)
        private static Anime ToAnime(XmlNode node){
            if(node.Name != "entry") throw new ApiFormatException("The node is not an entry node.");
            if(!node.HasChildNodes) throw new ApiFormatException("The node has no information.");
            try {
                var id = int.Parse(node.SelectSingleNode(".//id/text()").Value);
                var title = node.SelectSingleNode(".//title/text()").Value;
                var englishnode = node.SelectSingleNode(".//english/text()");
                var english = (englishnode == null) ? string.Empty : englishnode.Value;
                var synonymnode = node.SelectSingleNode(".//synonyms/text()");
                var synonyms = synonymnode == null ? new string[0] : Regex.Split(synonymnode.Value, "; ");
                var episodes = int.Parse(node.SelectSingleNode(".//episodes/text()").Value);
                var score = double.Parse(node.SelectSingleNode(".//score/text()").Value);
                var type = ResolveAnimeType(node.SelectSingleNode(".//type/text()").Value);
                var status =
                    ResolveAnimeRunningStatus(node.SelectSingleNode(".//status/text()").Value);
                var startstring = node.SelectSingleNode(".//start_date/text()").Value;
                var endstring = node.SelectSingleNode(".//end_date/text()").Value;
                // If there is no exact date, it says 00-00. Throws an exception!
                var start = ParseDateTime(startstring);
                var end = ParseDateTime(endstring);
                var synopsisnode = node.SelectSingleNode(".//synopsis/text()");
                var synopsis = (synopsisnode == null) ? string.Empty : synopsisnode.Value;                
                var urlnode = node.SelectSingleNode(".//image/text()");
                var url = (urlnode == null) ? string.Empty : urlnode.Value;
                //var anilist = AniList.GetAniListAnimeEquiv(title, type, episodes).Result;
                return new Anime(id, title, english, "Japanese Title", synonyms, episodes, new Dictionary<int, DateTime>(), score, type, status, start, end, synopsis,
                    url, "MyAnimeList");
            }
            catch(NullReferenceException) {
                throw new ApiFormatException("The node was missing a required value.");
            }
        }

        // convert a node from an old API call into an Anime object
        private static Anime ToAnimeFromOld(XmlNode node){
            if(node.Name != "anime") throw new ApiFormatException("The node is not an anime node");
            if(!node.HasChildNodes) throw new ApiFormatException("The anime node has no information");
            try {
                var id = int.Parse(node.SelectSingleNode(".//series_animedb_id//text()").Value);
                var title = node.SelectSingleNode(".//series_title/text()").Value;
                var synonymnode = node.SelectSingleNode(".//series_synonyms/text()");
                var synonyms = synonymnode == null ? new string[0] : Regex.Split(synonymnode.Value, "; ");
                var type = ResolveAnimeType(node.SelectSingleNode(".//series_type/text()").Value);
                var episodes = int.Parse(node.SelectSingleNode(".//series_episodes/text()").Value);
                var status = ResolveAnimeRunningStatus(node.SelectSingleNode(".//series_status/text()").Value);
                var startstring = node.SelectSingleNode(".//series_start/text()").Value;
                var endstring = node.SelectSingleNode(".//series_end/text()").Value;
                var seriesStart = ParseDateTime(startstring);
                var seriesEnd = ParseDateTime(endstring);
                var url = node.SelectSingleNode(".//series_image/text()").Value;
                //var anilist = AniList.GetAniListAnimeEquiv(title, type, episodes).Result;
                var result = new Anime(
                    id, title, "English title", "Japanese Title", synonyms, episodes, new Dictionary<int, DateTime>(), 0, type, status, seriesStart, seriesEnd,
                    string.Empty, url, "MyAnimeList") {
                    CurrentEpisode = int.Parse(node.SelectSingleNode(".//my_watched_episodes/text()").Value)
                };

                // User information
                startstring = node.SelectSingleNode(".//my_start_date/text()").Value;
                endstring = node.SelectSingleNode(".//my_finish_date/text()").Value;
                result.UserStart = ParseDateTime(startstring);
                result.UserEnd = ParseDateTime(endstring);
                result.UserScore = int.Parse(node.SelectSingleNode(".//my_score/text()").Value);
                result.ListStatus = ResolveListStatus(node.SelectSingleNode(".//my_status/text()").Value);
                return result;
            }
            catch(NullReferenceException) {
                throw new ApiFormatException("The anime node was missing a required value.");
            }
        }
        private static Anime.ShowTypes ResolveAnimeType(string type){
            switch (type) {
                case "Special": case "4":
                    return Anime.ShowTypes.Special;
                case "Movie": case "3":
                    return Anime.ShowTypes.Movie;
                case "OVA": case "2":
                    return Anime.ShowTypes.Ova;
                case "ONA": case "5":
                    return Anime.ShowTypes.Ona;
                case "Music": case "6":
                    return Anime.ShowTypes.Music;
                default:
                    return Anime.ShowTypes.Tv;
            }
        }

        private static Anime.RunningStatuses ResolveAnimeRunningStatus(string status){
            switch (status) {
                case "Finished Airing": case "2":
                    return Anime.RunningStatuses.Completed;
                case "Currently Airing": case "1":
                    return Anime.RunningStatuses.Airing;
                default:
                    return Anime.RunningStatuses.NotYetAired;
            }
        }

        private static Manga ToManga(XmlNode node) {
            if(node.Name != "entry")
                throw new ApiFormatException("The node received was not a manga entry node");
            if(!node.HasChildNodes)
                throw new ApiFormatException("The node received contained no information");

            try {
                var id = int.Parse(node.SelectSingleNode(".//id/text()").Value);
                var title = node.SelectSingleNode(".//title/text()").Value;
                var englishnode = node.SelectSingleNode(".//english/text()");
                var english = (englishnode == null) ? string.Empty : englishnode.Value;
                var synonymnode = node.SelectSingleNode(".//synonyms/text()");
                var synonyms = synonymnode == null ? new string[0] : Regex.Split(synonymnode.Value, "; ");
                var chapters = int.Parse(node.SelectSingleNode(".//chapters/text()").Value);
                var volumes = int.Parse(node.SelectSingleNode(".//volumes/text()").Value);
                var score = double.Parse(node.SelectSingleNode(".//score/text()").Value);
                var type = ResolveMangaType(node.SelectSingleNode(".//type/text()").Value);
                var status = ResolveMangaStatus(node.SelectSingleNode(".//status/text()").Value);
                var startstring = node.SelectSingleNode(".//start_date/text()").Value;
                var endstring = node.SelectSingleNode(".//end_date/text()").Value;
                var start = ParseDateTime(startstring);
                var end = ParseDateTime(endstring);
                var synopsisnode = node.SelectSingleNode(".//synopsis/text()");
                var synopsis = (synopsisnode == null) ? string.Empty : synopsisnode.Value;
                var urlnode = node.SelectSingleNode(".//image/text()");
                var url = (urlnode == null) ? string.Empty : urlnode.Value;
                return new Manga(id, title, english, string.Empty, synonyms, chapters, volumes, score, type, status, start, end,
                    synopsis, url);
            }
            catch(NullReferenceException) {
                throw new ApiFormatException("One or more required nodes were missing from the response.");
            }
        }

        private static Manga ToMangaFromOld(XmlNode node){
            if(node.Name != "manga")
                throw new ApiFormatException("The node received was not a manga entry node");
            if(!node.HasChildNodes)
                throw new ApiFormatException("The node received contained no information");

            try {
                var id = int.Parse(node.SelectSingleNode(".//series_mangadb_id/text()").Value);
                var title = node.SelectSingleNode(".//series_title/text()").Value;
                var synonymnode = node.SelectSingleNode(".//series_synonyms/text()");
                var synonyms = synonymnode == null ? new string[0] : Regex.Split(synonymnode.Value, "; ");
                var type = ResolveMangaType(node.SelectSingleNode(".//series_type/text()").Value);
                var chapters = int.Parse(node.SelectSingleNode(".//series_chapters/text()").Value);
                var volumes = int.Parse(node.SelectSingleNode(".//series_volumes/text()").Value);
                var status = ResolveMangaStatus(node.SelectSingleNode(".//series_status/text()").Value);
                var startstring = node.SelectSingleNode(".//series_start/text()").Value;
                var endstring = node.SelectSingleNode(".//series_end/text()").Value;
                var start = ParseDateTime(startstring);
                var end = ParseDateTime(endstring);
                var url = node.SelectSingleNode(".//series_image/text()")?.Value;
                var result = new Manga(id, title, string.Empty, string.Empty, synonyms, chapters, volumes, 0.0, type, status, start,
                    end, string.Empty, url);
                result.CurrentChapter = int.Parse(node.SelectSingleNode(".//my_read_chapters/text()").Value);
                result.CurrentVolume = int.Parse(node.SelectSingleNode(".//my_read_volumes/text()").Value);
                startstring = node.SelectSingleNode(".//my_start_date/text()").Value;
                endstring = node.SelectSingleNode(".//my_finish_date/text()").Value;
                result.UserStart = ParseDateTime(startstring);
                result.UserEnd = ParseDateTime(endstring);
                result.UserScore = int.Parse(node.SelectSingleNode(".//my_score/text()").Value);
                result.ListStatus = ResolveListStatus(node.SelectSingleNode(".//my_status/text()").Value);
                return result;
            }
            catch(NullReferenceException) {
                throw new ApiFormatException("One or more required nodes were missing from the response.");
            }
        }

        private static Manga.MangaTypes ResolveMangaType(string type){
            switch(type) {
                case "One-Shot": case "3":
                    return Manga.MangaTypes.OneShot;
                case "Novel": case "2":
                    return Manga.MangaTypes.Novel;
                case "Manhwa": case "5":
                    return Manga.MangaTypes.Manhwa;
                case "Manhua": case "6":
                    return Manga.MangaTypes.Manhua;
                case "Doujinshi": case "4":
                    return Manga.MangaTypes.Doujinshi;
                default:
                    return Manga.MangaTypes.Manga;
            }
        }

        private static Manga.RunningStatuses ResolveMangaStatus(string status){
            switch(status) {
                case "Finished": case "2":
                    return Manga.RunningStatuses.Finished;
                case "Not yet published": case "3":
                    return Manga.RunningStatuses.NotYetPublished;
                default:
                    return Manga.RunningStatuses.Publishing;
            }
        }

        private static ApiEntry.ListStatuses ResolveListStatus(string status){
            switch(status) {
                case "2":
                    return ApiEntry.ListStatuses.Completed;
                case "3":
                    return ApiEntry.ListStatuses.OnHold;
                case "4":
                    return ApiEntry.ListStatuses.Dropped;
                case "6":
                    return ApiEntry.ListStatuses.Planned;
                default:
                    return ApiEntry.ListStatuses.Current;
            }
        }

        private static DateTime ParseDateTime(string date) {
            DateTime dt;
            try {
                dt = DateTime.ParseExact(date, DateFormat, CultureInfo.InvariantCulture);
            }
            catch(FormatException) {
                if(date == DefaultDate)
                    dt = DateTime.MinValue;
                else if(Regex.IsMatch(date, "[0-9]{4}-00-00")) {
                    var r = Regex.Match(date, "([0-9]{4})-00-00");
                    dt = new DateTime(int.Parse(r.Groups[1].Value), 1, 1);
                }
                else if(Regex.IsMatch(date, "[0-9]{4}-[0-9]{2}-00")) {
                    var r = Regex.Match(date, "([0-9]{4})-([0-9]{2})-00");
                    dt = new DateTime(int.Parse(r.Groups[1].Value), int.Parse(r.Groups[2].Value), 1);
                }
                else
                    dt = DateTime.MinValue;
            }

            return dt;
        }

        ~MyAnimeList(){
            _client.Dispose();
            _clientLogin = null;
        }
    }
}