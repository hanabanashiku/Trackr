﻿using System;
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

namespace Trackr.Api {
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
            Username = _clientLogin.Username;
        }

        /// <summary>
        /// Verify provided credentials with MyAnimeList.
        /// </summary>
        /// <returns>true if credentials are valid</returns>
        /// <exception cref="ApiRequestException" />
        public async Task<bool> VerifyCredentials(){
            var response = await _client.GetAsync(Path.Combine(UrlBase, "account", "verify_credentials.xml"));
            return response.Content.ReadAsStringAsync().Result != "Invalid credentials";
        }

        /// <summary>
        /// Add an anime to the authenticated user's list.
        /// </summary>
        /// <param name="id">The MAL ID number of the given anime</param>
        /// <param name="listStatus">The listStatus to add it under (default is Currently Watching)</param>
        /// <returns>true on success (201), false on failure (400).</returns>
        public async Task<bool> AddAnime(int id, ApiEntry.ListStatuses listStatus = ApiEntry.ListStatuses.Current){
            var data = new FormUrlEncodedContent(new [] {
                new KeyValuePair<string, string>("data",
                   "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                   "<entry>" +
                   "<episode>0</episode>" +
                   "<status>" + (int)listStatus + "</status>" +
                   "</entry>")
            });
            var response = await _client.PostAsync(Path.Combine(UrlBase, "animelist", "add", id+".xml"), data);
            if(response.StatusCode == HttpStatusCode.BadRequest) // the anime is probably already there
                throw new ApiRequestException(response.Content.ReadAsStringAsync().Result);
            return response.StatusCode == HttpStatusCode.Created;
        }

        /// <summary>
        /// Remove an anime from the authenticated user's list.
        /// </summary>
        /// <param name="id">The anime to remove</param>
        /// <returns>true on success</returns>
        public async Task<bool> RemoveAnime(int id){
            var response = await _client.DeleteAsync(Path.Combine(UrlBase, "animelist", "delete", id + ".xml"));
            return response.Content.ReadAsStringAsync().Result == "Deleted";
        }

        /// <summary>
        /// Search for an anime in the MyAnimeList database.
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
        /// Update the given entry on the authenticated user's list.
        /// </summary>
        /// <param name="anime">The anime to update, with updated values.</param>
        /// <returns>true on success</returns>
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
            return response.Content.ReadAsStringAsync().Result.Contains("Updated");
        }

        /// <summary>
        /// Get the authenticated user's anime list.
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
            if(response.StatusCode == HttpStatusCode.BadRequest)
                throw new ApiRequestException(response.Content.ReadAsStringAsync().Result);
            return response.StatusCode == HttpStatusCode.Created;
        }

        /// <summary>
        /// Remove a manga from the authenticated user's list.
        /// </summary>
        /// <param name="id">The MAL ID of the manga to remove.</param>
        /// <returns>true on success</returns>
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
        /// <exception cref="ApiFormatException">if the response is malformed.</exception>
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
                int id = int.Parse(node.SelectSingleNode(".//id/text()").Value);
                string title = node.SelectSingleNode(".//title/text()").Value;
                var englishnode = node.SelectSingleNode(".//english/text()");
                string english = (englishnode == null) ? string.Empty : englishnode.Value;
                XmlNode synonymnode = node.SelectSingleNode(".//synonyms/text()");
                var synonyms = synonymnode == null ? new string[0] : Regex.Split(synonymnode.Value, "; ");
                int episodes = int.Parse(node.SelectSingleNode(".//episodes/text()").Value);
                double score = double.Parse(node.SelectSingleNode(".//score/text()").Value);
                Anime.ShowTypes type = ResolveAnimeType(node.SelectSingleNode(".//type/text()").Value);
                Anime.RunningStatuses status =
                    ResolveAnimeRunningStatus(node.SelectSingleNode(".//status/text()").Value);
                string startstring = node.SelectSingleNode(".//start_date/text()").Value;
                string endstring = node.SelectSingleNode(".//end_date/text()").Value;
                DateTime start = (startstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(startstring, DateFormat, CultureInfo.InvariantCulture);
                DateTime end = (endstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(endstring, DateFormat, CultureInfo.InvariantCulture);
                var synopsisnode = node.SelectSingleNode(".//synopsis/text()");
                string synopsis = (synopsisnode == null) ? string.Empty : synopsisnode.Value;                var urlnode = node.SelectSingleNode(".//image/text()");
                string url = (urlnode == null) ? string.Empty : urlnode.Value;
                return new Anime(id, title, english, synonyms, episodes, score, type, status, start, end, synopsis,
                    url);
            }
            catch(NullReferenceException e) {
                throw new ApiFormatException("The node was missing a required value.");
            }
        }

        // convert a node from an old API call into an Anime object
        private static Anime ToAnimeFromOld(XmlNode node){
            if(node.Name != "anime") throw new ApiFormatException("The node is not an anime node");
            if(!node.HasChildNodes) throw new ApiFormatException("The anime node has no information");
            try {
                int id = int.Parse(node.SelectSingleNode(".//series_animedb_id//text()").Value);
                string title = node.SelectSingleNode(".//series_title/text()").Value;
                var synonymnode = node.SelectSingleNode(".//series_synonyms/text()");
                var synonyms = synonymnode == null ? new string[0] : Regex.Split(synonymnode.Value, "; ");
                var type = ResolveAnimeType(node.SelectSingleNode(".//series_type/text()").Value);
                int episodes = int.Parse(node.SelectSingleNode(".//series_episodes/text()").Value);
                var status = ResolveAnimeRunningStatus(node.SelectSingleNode(".//series_status/text()").Value);
                string startstring = node.SelectSingleNode(".//series_start/text()").Value;
                string endstring = node.SelectSingleNode(".//series_end/text()").Value;
                DateTime seriesStart = (startstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(startstring.Replace("00", "01"), DateFormat, CultureInfo.InvariantCulture);
                DateTime seriesEnd = (endstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(endstring.Replace("00", "01"), DateFormat, CultureInfo.InvariantCulture);
                string url = node.SelectSingleNode(".//series_image/text()").Value;
                Anime result = new Anime(
                    id, title, string.Empty, synonyms, episodes, 0.0, type, status, seriesStart, seriesEnd,
                    string.Empty, url) {
                    CurrentEpisode = int.Parse(node.SelectSingleNode(".//my_watched_episodes/text()").Value)
                };

                // User information
                startstring = node.SelectSingleNode(".//my_start_date/text()").Value;
                endstring = node.SelectSingleNode(".//my_finish_date/text()").Value;
                result.UserStart = (startstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(startstring, DateFormat, CultureInfo.InvariantCulture);
                result.UserEnd = (endstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(endstring, DateFormat, CultureInfo.InvariantCulture);
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
                int id = int.Parse(node.SelectSingleNode(".//id/text()").Value);
                string title = node.SelectSingleNode(".//title/text()").Value;
                var englishnode = node.SelectSingleNode(".//english/text()");
                string english = (englishnode == null) ? string.Empty : englishnode.Value;
                var synonymnode = node.SelectSingleNode(".//synonyms/text()");
                var synonyms = synonymnode == null ? new string[0] : Regex.Split(synonymnode.Value, "; ");
                int chapters = int.Parse(node.SelectSingleNode(".//chapters/text()").Value);
                int volumes = int.Parse(node.SelectSingleNode(".//volumes/text()").Value);
                double score = double.Parse(node.SelectSingleNode(".//score/text()").Value);
                Manga.MangaTypes type = ResolveMangaType(node.SelectSingleNode(".//type/text()").Value);
                Manga.RunningStatuses status = ResolveMangaStatus(node.SelectSingleNode(".//status/text()").Value);
                string startstring = node.SelectSingleNode(".//start_date/text()").Value;
                string endstring = node.SelectSingleNode(".//end_date/text()").Value;
                DateTime start = (startstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(startstring.Replace("00", "01"), DateFormat, CultureInfo.InvariantCulture);
                DateTime end = (endstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(endstring.Replace("00", "01"), DateFormat, CultureInfo.InvariantCulture);
                var synopsisnode = node.SelectSingleNode(".//synopsis/text()");
                string synopsis = (synopsisnode == null) ? string.Empty : synopsisnode.Value;
                var urlnode = node.SelectSingleNode(".//image/text()");
                string url = (urlnode == null) ? string.Empty : urlnode.Value;
                return new Manga(id, title, english, synonyms, chapters, volumes, score, type, status, start, end,
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
                int id = int.Parse(node.SelectSingleNode(".//series_mangadb_id/text()").Value);
                string title = node.SelectSingleNode(".//series_title/text()").Value;
                var synonymnode = node.SelectSingleNode(".//series_synonyms/text()");
                var synonyms = synonymnode == null ? new string[0] : Regex.Split(synonymnode.Value, "; ");
                Manga.MangaTypes type = ResolveMangaType(node.SelectSingleNode(".//series_type/text()").Value);
                int chapters = int.Parse(node.SelectSingleNode(".//series_chapters/text()").Value);
                int volumes = int.Parse(node.SelectSingleNode(".//series_volumes/text()").Value);
                Manga.RunningStatuses status = ResolveMangaStatus(node.SelectSingleNode(".//series_status/text()").Value);
                string startstring = node.SelectSingleNode(".//series_start/text()").Value;
                string endstring = node.SelectSingleNode(".//series_end/text()").Value;
                DateTime start = (startstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(startstring.Replace("00", "01"), DateFormat, CultureInfo.InvariantCulture);
                DateTime end = (endstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(endstring.Replace("00", "01"), DateFormat, CultureInfo.InvariantCulture);
                string url = node.SelectSingleNode(".//series_image/text()").Value;
                Manga result = new Manga(id, title, string.Empty, synonyms, chapters, volumes, 0.0, type, status, start,
                    end, string.Empty, url);
                result.CurrentChapter = int.Parse(node.SelectSingleNode(".//my_read_chapters/text()").Value);
                result.CurrentVolume = int.Parse(node.SelectSingleNode(".//my_read_volumes/text()").Value);
                startstring = node.SelectSingleNode(".//my_start_date/text()").Value;
                endstring = node.SelectSingleNode(".//my_finish_date/text()").Value;
                result.UserStart = (startstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(startstring.Replace("00", "01"), DateFormat, CultureInfo.InvariantCulture);
                result.UserEnd = (startstring == DefaultDate)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(endstring.Replace("00", "01"), DateFormat, CultureInfo.InvariantCulture);
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

        ~MyAnimeList(){
            _client.Dispose();
            _clientLogin = null;
        }
    }
}