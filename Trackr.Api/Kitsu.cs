using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Json;
using System.Linq;
using System.Threading.Tasks;

using Trackr.Core;

namespace Trackr.Api {
    /// <summary>
    /// A class for handling instances of Kitsu (formerly Hummingbird) accounts.
    /// </summary>
    /// <remarks>This currently uses the undocumented Edge API. The official API is a WIP.</remarks>
    public class Kitsu : Api, IAnime, IManga {
        public new string Name { get; } = "Kitsu";
        public new string Username => _clientLogin.Username;

        private const string ContentType = "application/vnd.api+json";
        private const string AuthUrl = "https://kitsu.io/api/oauth/token";
        // These are defaults, as app registration is not yet possible on Kitsu
        private const string ClientId = "dd031b32d2f56c990b1425efe6c42ad847e7fe3ab46bf1299f05ecd856bdb7dd";
        private const string ClientSecret = "54d7307928f63414defd96399fc31ba847961ceaecef3a5fd93144e960c0e151";
        private const string UrlBase = "https://kitsu.io/api/edge";
        private const string LibraryEntries = UrlBase + "/library-entries";
        private const string AnimeItems = UrlBase + "/anime";
        private const string MangaItems = UrlBase + "/manga";
        private const string DateFormat = "yyyy-MM-dd";

        private readonly HttpClient _client;
        private readonly UserPass _clientLogin;
        private DateTime _expiration;
        private int _userId;

        public Kitsu(UserPass credentials){
            _clientLogin = credentials;
            _client = new HttpClient();
            _client.DefaultRequestHeaders
                .TryAddWithoutValidation("Accept", ContentType);
            _client.DefaultRequestHeaders
                .TryAddWithoutValidation("Content-Type", ContentType);
            _expiration = DateTime.Now;
        }

        private async Task<bool> Authenticate(){
            var data = new FormUrlEncodedContent(new [] {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", _clientLogin.Username),
                new KeyValuePair<string, string>("password", _clientLogin.Password),
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("client_secret", ClientSecret)
            });
            var response = await _client.PostAsync(AuthUrl, data);
            if(response.StatusCode != HttpStatusCode.OK)
                return false;
            // store the token in the client and make a note of the expiration time.
            var json = JsonValue.Parse(response.Content.ReadAsStringAsync().Result);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(json["token_type"], json["access_token"]);
            _expiration = DateTime.Now.AddSeconds(double.Parse(json["expires_in"]));
            return true;
        }

        /// <summary>
        /// Running this function from the Kitsu API authenticates with the Kitsu OAuth2 service.
        /// </summary>
        /// <returns>True on success (200OK), false on failure (400, 401)</returns>
        /// <remarks>Authentication will be done implicitly  after the token expires.</remarks>
        /// <remarks>This method must be run at least once to resolve the userid of the user.</remarks>
        public async Task<bool> VerifyCredentials(){
            if(await Authenticate() == false)
                return false;

            // some calls require the user's id
            var response = await _client.GetAsync(UrlBase + $"/users?filter[name]={Username}");
            var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
            if(json["data"].Count == 0) return false;
            foreach(var i in (JsonArray)json["data"])
                if(i["attributes"]["name"] == Username) {
                    _userId = i["id"];
                    return true;
                }
            return false;
        }

        public async Task<bool> AddAnime(int id, ApiEntry.ListStatuses status){
            AuthenticationCheck();

            var data = new JsonObject() {
                ["data"] = new JsonObject {
                    ["type"] = "libraryEntries",
                    ["attributes"] = new JsonObject() {
                        ["status"] = FromListStatus(status)
                    },
                    ["relationships"] = new JsonObject() {
                        ["user"] = new JsonObject() {
                            ["data"] = new JsonObject() {
                                ["id"] = _userId,
                                ["type"] = "users"
                            }
                        }
                    },
                    ["media"] = new JsonObject() {
                        ["data"] = new JsonObject() {
                            ["id"] = id,
                            ["type"] = "anime"
                        }
                    }
                }
            };

            var response = await _client.PostAsync(LibraryEntries, new StringContent(data));
            return response.StatusCode == HttpStatusCode.OK;
        }

        /// <summary>
        /// Remove anime from the authenticated user's list
        /// </summary>
        /// <param name="id">The ID of the anime to remove.</param>
        /// <returns>True on success</returns>
        public async Task<bool> RemoveAnime(int id){
            AuthenticationCheck();

            int entryId = await GetEntryId(id, "Anime");

            if(entryId == -1) return true; // the entry wasn't there
            if(entryId == -2) return false; // something went wrong

            var response = await _client.DeleteAsync(LibraryEntries + $"/{entryId}");
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Search for an anime in the Kitsu database.
        /// </summary>
        /// <param name="keywords">The search term to use</param>
        /// <returns>A list of all results.</returns>
        /// <exception cref="ApiRequestException">If a request fails.</exception>
        /// <remarks>Kitsu will only return 20 items at a time, so multiple requests may be sent.</remarks>
        public async Task<List<Anime>> FindAnime(string keywords){
            List<Anime> ret = new List<Anime>();
            string url = AnimeItems + $"?filter%5Btext%5D={keywords}&page%5Blimit%5D=20&page%5Boffset%5D=0";
            string next = string.Empty;
            do {
                if(next != string.Empty) url = next;
                var response = await _client.GetAsync(url);
                if(response.StatusCode != HttpStatusCode.OK)
                    throw new ApiRequestException(response.StatusCode.ToString());
                var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
                ret.AddRange(from i in (JsonArray)json["data"] select ToAnime(i));
                if(json["links"]["next"] == null) return ret;
                next = json["links"]["next"];
            } while(true);
        }

        /// <summary>
        /// Update the anime on the authenticated user's list.
        /// </summary>
        /// <param name="a">The anime to update</param>
        /// <returns>True on success</returns>
        public async Task<bool> UpdateAnime(Anime a){
            AuthenticationCheck();
            if(a.ListStatus == ApiEntry.ListStatuses.NotInList)
                return await RemoveAnime(a.Id);

            if(a.ListStatus == ApiEntry.ListStatuses.Completed)
                a.CurrentEpisode = a.Episodes;

            int id = await GetEntryId(a.Id, "Anime");
            if(id == -2) return false;
            if(id == -1) {
                await AddAnime(a.Id, a.ListStatus);
                id = await GetEntryId(a.Id, "Anime");
                if(id < 0) return false;
            }

            var json = new JsonObject() {
                ["data"] = new JsonObject() {
                    ["id"] = id,
                    ["type"] = "libraryEntries",
                    ["attributes"] = new JsonObject() {
                        ["status"] = FromListStatus(a.ListStatus),
                        ["progress"] = a.CurrentEpisode
                    }
                }
            };
            // Convert from 0-10 to 0-5
            if(a.UserScore != 0) json["data"]["attributes"]["rating"] = a.UserScore / 2;

            var response = await _client.PostAsync(LibraryEntries, new StringContent(json));
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get the authenticated user's anime list.
        /// </summary>
        /// <returns>A list of all anime in the user's list from the server.</returns>
        /// <exception cref="ApiRequestException">If a request fails.</exception>
        /// <remarks>Kitsu will only return 500 items at a time, so multiple requests may be sent.</remarks>
        public async Task<List<Anime>> PullAnimeList(){
            List<Anime> ret = new List<Anime>();
            string url = AnimeItems +
                         $"?filter%5Buser_id%5D={_userId}&filter%5Bmedia_type%5D=Anime&filter%5Bstatus%5D=1,2,3,4,5&include=media&page%5Blimit%5D=500&page%5Boffset%5D=0";
            string next = string.Empty;
            do {
                if(next != string.Empty) url = next;
                var response = await _client.GetAsync(url);
                if(response.StatusCode != HttpStatusCode.OK)
                    throw new ApiRequestException(response.StatusCode.ToString());
                var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
                foreach(var i in (JsonArray)json["data"]) {
                    var a = ToAnime(i["included"][0]);
                    a.ListStatus = ToListStatus(i["attributes"]["status"]);
                    a.CurrentEpisode = i["attributes"]["progress"];
                    a.UserScore = (int)((double)i["attributes"]["rating"])*2; // always 0-5 with half steps.
                    // Kitsu does not have UserStart/UserEnd items, so the default will be used.
                }
                if(json["next"] == null) return ret;
                next = json["next"];
            } while(true);
        }

        public async Task<bool> AddManga(int id, ApiEntry.ListStatuses status){
            AuthenticationCheck();

            var data = new JsonObject() {
                ["data"] = new JsonObject {
                    ["type"] = "libraryEntries",
                    ["attributes"] = new JsonObject() {
                        ["status"] = FromListStatus(status)
                    },
                    ["relationships"] = new JsonObject() {
                        ["user"] = new JsonObject() {
                            ["data"] = new JsonObject() {
                                ["id"] = _userId,
                                ["type"] = "users"
                            }
                        }
                    },
                    ["media"] = new JsonObject() {
                        ["data"] = new JsonObject() {
                            ["id"] = id,
                            ["type"] = "manga"
                        }
                    }
                }
            };

            var response = await _client.PostAsync(LibraryEntries, new StringContent(data));
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Remove the manga from the authenticated user's list
        /// </summary>
        /// <param name="id">The ID of the manga to remove.</param>
        /// <returns>True on success</returns>
        public async Task<bool> RemoveManga(int id){
            AuthenticationCheck();

            int entryId = await GetEntryId(id, "Manga");

            if(entryId == -1) return true; // the entry wasn't there
            if(entryId == -2) return false; // something went wrong

            var response = await _client.DeleteAsync(LibraryEntries + $"/{entryId}");
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Search for a manga in the Kitsu database.
        /// </summary>
        /// <param name="keywords">The search term to use</param>
        /// <returns>A list of all results.</returns>
        /// <exception cref="ApiRequestException">If a request fails.</exception>
        /// <remarks>Kitsu will only return 20 items at a time, so multiple requests may be sent.</remarks>
        public async Task<List<Manga>> FindManga(string keywords){
            List<Manga> ret = new List<Manga>();
            string url = MangaItems + $"?filter%5Btext%5D={keywords}&page%5Blimit%5D=20&page%5Boffset%5D=0";
            string next = string.Empty;
            do {
                if(next != string.Empty) url = next;
                var response = await _client.GetAsync(url);
                if(response.StatusCode != HttpStatusCode.OK)
                    throw new ApiRequestException(response.StatusCode.ToString());
                var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
                ret.AddRange(from i in (JsonArray)json["data"] select ToManga(i));
                if(json["links"]["next"] == null) return ret;
                next = json["links"]["next"];
            } while(true);
        }

        /// <summary>
        /// Update the manga on the authenticated user's list.
        /// </summary>
        /// <param name="m">The manga to update</param>
        /// <returns>True on success</returns>
        public async Task<bool> UpdateManga(Manga m){
            AuthenticationCheck();
            if(m.ListStatus == ApiEntry.ListStatuses.NotInList)
                return await RemoveManga(m.Id);

            if(m.ListStatus == ApiEntry.ListStatuses.Completed)
                m.CurrentChapter = m.Chapters;

            int id = await GetEntryId(m.Id, "Manga");
            if(id == -2) return false;
            if(id == -1) {
                await AddManga(m.Id, m.ListStatus);
                id = await GetEntryId(m.Id, "Manga");
                if(id < 0) return false;
            }

            var json = new JsonObject() {
                ["data"] = new JsonObject() {
                    ["id"] = id,
                    ["type"] = "libraryEntries",
                    ["attributes"] = new JsonObject() {
                        ["status"] = FromListStatus(m.ListStatus),
                        ["progress"] = m.CurrentChapter
                    }
                }
            };
            // Convert from 0-10 to 0-5
            if(m.UserScore != 0) json["data"]["attributes"]["rating"] = m.UserScore / 2;

            var response = await _client.PostAsync(LibraryEntries, new StringContent(json));
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get the authenticated user's anime list.
        /// </summary>
        /// <returns>A list of all anime in the user's list from the server.</returns>
        /// <exception cref="ApiRequestException">If a request fails.</exception>
        /// <remarks>Kitsu will only return 500 items at a time, so multiple requests may be sent.</remarks>
        public async Task<List<Manga>> PullMangaList(){
            List<Manga> ret = new List<Manga>();
            string url = MangaItems +
                         $"?filter%5Buser_id%5D={_userId}&filter%5Bmedia_type%5D=Manga&filter%5Bstatus%5D=1,2,3,4,5&include=media&page%5Blimit%5D=500&page%5Boffset%5D=0";
            string next = string.Empty;
            do {
                if(next != string.Empty) url = next;
                var response = await _client.GetAsync(url);
                if(response.StatusCode != HttpStatusCode.OK)
                    throw new ApiRequestException(response.StatusCode.ToString());
                var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
                foreach(var i in (JsonArray)json["data"]) {
                    var m = ToManga(i["included"][0]);
                    m.ListStatus = ToListStatus(i["attributes"]["status"]);
                    m.CurrentChapter = i["attributes"]["progress"];
                    m.UserScore = (int)((double)i["attributes"]["rating"])*2; // always 0-5 with half steps.
                    // Kitsu does not have UserStart/UserEnd items, so the default will be used.
                }
                if(json["next"] == null) return ret;
                next = json["next"];
            } while(true);
        }

        private static string FromListStatus(ApiEntry.ListStatuses status){
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch(status) {
                case ApiEntry.ListStatuses.Completed:
                    return "completed";
                case ApiEntry.ListStatuses.Current:
                    return "current";
                case ApiEntry.ListStatuses.Dropped:
                    return "dropped";
                case ApiEntry.ListStatuses.Planned:
                    return "planned";
                case ApiEntry.ListStatuses.OnHold:
                    return "on_hold";
                // NotInList - this should never be sent to the server
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        private static ApiEntry.ListStatuses ToListStatus(string status){
            switch(status) {
                case "completed":
                    return ApiEntry.ListStatuses.Completed;
                case "current":
                    return ApiEntry.ListStatuses.Current;
                case "dropped":
                    return ApiEntry.ListStatuses.Dropped;
                case "planned":
                    return ApiEntry.ListStatuses.Planned;
                case "on_hold":
                    return ApiEntry.ListStatuses.OnHold;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        private static Anime.ShowTypes ToShowType(string type){
            switch(type) {
                case "special":
                    return Anime.ShowTypes.Special;
                case "OVA":
                    return Anime.ShowTypes.Ova;
                case "ONA":
                    return Anime.ShowTypes.Ona;
                case "movie":
                    return Anime.ShowTypes.Movie;
                case "music":
                    return Anime.ShowTypes.Music;
                case "TV":
                    return Anime.ShowTypes.Tv;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static Manga.MangaTypes ToMangaType(string type){
            switch(type) {
                case "manga":
                    return Manga.MangaTypes.Manga;
                case "novel":
                    return Manga.MangaTypes.Novel;
                case "manhua":
                    return Manga.MangaTypes.Manhua;
                case "oneshot":
                    return Manga.MangaTypes.OneShot;
                case "doujin":
                    return Manga.MangaTypes.Doujinshi;
                case "manhwa": // Kitsu doesn't actually have this
                    return Manga.MangaTypes.Manhwa;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        // On Kitsu, each item has a unique entry id. For some operations, we need it.
        // type is either 'Anime' or 'Manga'
        private async Task<int> GetEntryId(int entry, string type){
            var response = await _client.GetAsync(
                LibraryEntries + $"?filter[user_id]={_userId}&filter[media_type]={type}&filter[media_id]={entry}");
            if(response.StatusCode != HttpStatusCode.OK) return -2;
            var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
            if(json["data"].Count == 0) return -1;
            else return json["data"][0]["id"];
        }

        private static Anime ToAnime(JsonValue json){
            if(json["type"] != "anime")
                throw new ApiFormatException("Unexpected type encountered");

            string[] synonyms = new string[json["attributes"]["abbreviatedTitles"].Count];
            for(int i = 0; i < json["attributes"]["abbreviatedTitles"].Count; i++)
                synonyms[i] = json["attributes"]["abbreviatedTitles"][i];
            DateTime start = ToDateTime(json["attributes"]["startDate"]);
            DateTime end = ToDateTime(json["attributes"]["endDate"]);
            Anime.ShowTypes type = ToShowType(json["attributes"]["showType"]);
            Anime.RunningStatuses status;
            if(start == DateTime.MinValue) status = Anime.RunningStatuses.NotYetAired;
            else if(end == DateTime.MinValue) status = Anime.RunningStatuses.Airing;
            else status = Anime.RunningStatuses.Completed;

            return new Anime(json["id"], json["attributes"]["titles"]["en_jp"],
                json["attributes"]["titles"]["en"], json["attributes"]["titles"]["jp"],
                synonyms, json["attributes"]["episodeCount"], json["attributes"]["averageRating"],
                type, status, start, end, json["attributes"]["synopsis"], json["attributes"]["coverImage"]["original"]);
        }

        private static Manga ToManga(JsonValue json){
            if(json["type"] != "manga")
                throw new ApiFormatException("Unexpected type encountered");

            string[] synonyms = new string[json["attributes"]["abbreviatedTitles"].Count];
            for(int i = 0; i < json["attributes"]["abbreviatedTitles"].Count; i++)
                synonyms[i] = json["attributes"]["abbreviatedTitles"][i];
            DateTime start = ToDateTime(json["attributes"]["startDate"]);
            DateTime end = ToDateTime(json["attributes"]["endDate"]);
            Manga.MangaTypes type = ToMangaType(json["attributes"]["mangaType"]);
            Manga.RunningStatuses status;
            if(start == DateTime.MinValue) status = Manga.RunningStatuses.NotYetPublished;
            else if(end == DateTime.MinValue) status = Manga.RunningStatuses.Publishing;
            else status = Manga.RunningStatuses.Finished;

            return new Manga(json["id"], json["attributes"]["titles"]["en_jp"],
                json["attributes"]["titles"]["en"], json["attributes"]["titles"]["jp"],
                synonyms, json["attributes"]["episodeCount"], 0, json["attributes"]["averageRating"],
                type, status, start, end, json["attributes"]["synopsis"], json["attributes"]["coverImage"]["original"]);
        }

        private static DateTime ToDateTime(string date){
            return (date == null)
                ? DateTime.MinValue
                : DateTime.ParseExact(date, DateFormat, CultureInfo.InvariantCulture);
        }

        // when performing oauth functions, lets make sure that our token has not expired.
        private async void AuthenticationCheck(){
            if(_userId == 0 && !await VerifyCredentials())
                throw new ApiRequestException("[Kitsu] Could not verify user credentials");
            if(DateTime.Now >= _expiration) await Authenticate();
        }

        ~Kitsu(){
            _client.Dispose();
        }
    }
}