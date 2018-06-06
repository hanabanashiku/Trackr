using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Json;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Trackr.Core;

namespace Trackr.Api {
    /// <summary>
    /// A class for handling instances of Kitsu (formerly Hummingbird) accounts.
    /// </summary>
    /// <remarks>This currently uses the undocumented Edge API. The official API is a WIP.</remarks>
    public class Kitsu : Api, IAnime, IManga {
		public const string Identifier = "Kitsu";

        public override string Name { get; } = Identifier;
        public override string Username => _username;

        private const string ContentType = "application/vnd.api+json";
        private const string AuthUrl = "https://kitsu.io/api/oauth/token";

        private static string _clientId;
        private static string ClientId {
            get {
                if(_clientId == null) GetClientInfo();
                return _clientId;
            }
        }
        private static string _clientSecret;
        private static string ClientSecret {
            get {
                if(_clientSecret == null) GetClientInfo();
                return _clientSecret;
            }
        }
        private const string UrlBase = "https://kitsu.io/api/edge";
        private const string LibraryEntries = UrlBase + "/library-entries";
        private const string AnimeItems = UrlBase + "/anime";
        private const string MangaItems = UrlBase + "/manga";
        private const string EpisodeItems = UrlBase + "/episodes";
        private static readonly string[] DateFormat = new string[] { "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss.fffZ" };

        private readonly HttpClient _client;
        private readonly UserPass _clientLogin;
        private DateTime _expiration;
        private int _userId;
        private string _username;

        public Kitsu(UserPass credentials){
            _clientLogin = credentials;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ContentType));
            _expiration = DateTime.Now;
        }

        private async Task<bool> Authenticate(){
            var data = new FormUrlEncodedContent(new [] {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("client_secret", ClientSecret),
                new KeyValuePair<string, string>("username", _clientLogin.Username),
                new KeyValuePair<string, string>("password", _clientLogin.Password)
            });
            var response = await _client.PostAsync(AuthUrl, data);
            if(response.StatusCode != HttpStatusCode.OK) {
                Console.Write(response.Content.ReadAsStringAsync());
                return false;
            }
            // store the token in the client and make a note of the expiration time.
            var json = JsonValue.Parse(response.Content.ReadAsStringAsync().Result);
            if(json == null) return false;
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(json["token_type"], json["access_token"]);
            _expiration = DateTime.Now.AddSeconds(json["expires_in"]);
            return true;
        }

        /// <summary>
        /// Running this function from the Kitsu API authenticates with the Kitsu OAuth2 service.
        /// </summary>
        /// <returns>True on success (200OK), false on failure (400, 401)</returns>
        /// <remarks>Authentication will be done implicitly  after the token expires.</remarks>
        /// <remarks>This method must be run at least once to resolve the userid of the user.</remarks>
        public override async Task<bool> VerifyCredentials(){
            if(await Authenticate() == false)
                return false;
           
            // some calls require the user's id
            var response = await _client.GetAsync(UrlBase + "/users?filter[self]=true");
            var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
            Debug.WriteIf(!response.IsSuccessStatusCode, response.Content.ReadAsStringAsync(), "Kitsu WARNING");
            if(json == null || json["data"].Count == 0) return false;
            var data = ((JsonArray)json["data"]).First();
            _username = data["attributes"]["name"];
            _userId = data["id"];
            return true;
        }
        
        public async Task<bool> AddAnime(int id){
            AuthenticationCheck();

            var data = new JsonObject() {
                ["data"] = new JsonObject {
                    ["type"] = "libraryEntries",
                    ["attributes"] = new JsonObject() {
                        ["status"] = FromListStatus(ApiEntry.ListStatuses.Current)
                    },
                    ["relationships"] = new JsonObject() {
                        ["user"] = new JsonObject() {
                            ["data"] = new JsonObject() {
                                ["id"] = _userId,
                                ["type"] = "users"
                            }
                        },
                        ["media"] = new JsonObject() {
                            ["data"] = new JsonObject() {
                                ["type"] = "anime",
                                ["id"] = id
                            }
                        }
                    },
                }
            };
            
            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, LibraryEntries) {
                Content = new StringContent(data.ToString(), Encoding.UTF8, ContentType)
            });
            Debug.WriteIf(!response.IsSuccessStatusCode, response.Content.ReadAsStringAsync(), "Kitsu WARNING");
            return response.StatusCode == HttpStatusCode.Created;
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
                        },
                        ["media"] = new JsonObject() {
                            ["data"] = new JsonObject() {
                                ["type"] = "anime",
                                ["id"] = id
                            }
                        }
                    },
                }
            };
            
            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, LibraryEntries) {
                Content = new StringContent(data.ToString(), Encoding.UTF8, ContentType),
            });
            Debug.WriteIf(!response.IsSuccessStatusCode, response.Content.ReadAsStringAsync(), "Kitsu WARNING");
            return response.StatusCode == HttpStatusCode.Created;
        }

        /// <summary>
        /// Remove anime from the authenticated user's list
        /// </summary>
        /// <param name="id">The ID of the anime to remove.</param>
        /// <returns>True on success</returns>
        public async Task<bool> RemoveAnime(int id){
            AuthenticationCheck();

            var entryId = await GetEntryId(id, "anime");
            if(entryId == -1) return true; // the entry wasn't there
            if(entryId == -2) return false; // something went wrong

            var response = await _client.DeleteAsync(LibraryEntries + $"/{entryId}");
            Debug.WriteIf(!response.IsSuccessStatusCode, response.Content.ReadAsStringAsync(), "Kitsu WARNING");
            return response.IsSuccessStatusCode; // 200OK, 204NOCONTENT
        }

        /// <summary>
        /// Search for an anime in the Kitsu database.
        /// </summary>
        /// <param name="keywords">The search term to use</param>
        /// <returns>A list of all results.</returns>
        /// <exception cref="ApiRequestException">If a request fails.</exception>
        /// <remarks>Kitsu will only return 20 items at a time, so multiple requests may be sent.</remarks>
        /// <remarks>We shall limit this to 100 results (five requests). Kitsu's search algorithm seems rather generous..</remarks>
        public async Task<List<Anime>> FindAnime(string keywords){
            var ret = new List<Anime>(); // ?filter[text]=&page[limit]=20&page[offset]=0
            var url = AnimeItems + $"?filter%5Btext%5D={keywords}\u0026page%5Blimit\u00265D=20\u0026page%5Boffset%5D=0";
            string next = null;
            var count = 0; // limiting value
            do {
                if(next != null) url = next;
                var response = await _client.GetAsync(url);
                if(response.StatusCode != HttpStatusCode.OK) {
                    Debug.Write(await response.Content.ReadAsStringAsync(), "Kitsu WARNING");
                    throw new ApiRequestException(response.StatusCode.ToString());
                }
                var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
                if(json == null) throw new ApiRequestException("Null Response");
                ret.AddRange(from a in (JsonArray)json["data"] select ToAnime(a));
                next = json["links"].ContainsKey("next") ? json["links"]["next"] : null; // get the next page if there is one
                count++;
            } while(count <= 5 && next != null);
            return ret;
        }

        /// <summary>
        /// Get list of episodes
        /// </summary>
        /// <param name="animeId">The anime to get episodes for</param>
        /// <returns>A list of all episodes, indexed by episode number.</returns>
        /// <exception cref="ApiRequestException" />
        /// <remarks>The API will only return 20 episodes at a time, so for large episodes, this may make a lot of calls...</remarks>
        public static async Task<List<AnimeEpisode>> GetEpisodes(int animeId) {
            var ret = new List<AnimeEpisode>();
            var client = new HttpClient();
            var url = EpisodeItems + $"?filter%5BmediaId%5D={animeId}&page%5Blimit%5D=20";
            string next = null;
            do {
                if(next != null) url = next;
                var response = await client.GetAsync(url);
                if(response.StatusCode != HttpStatusCode.OK) {
                    Debug.Write(await response.Content.ReadAsStringAsync(), "Kitsu Episode WARNING");
                    throw new ApiRequestException(response.StatusCode.ToString());
                }
                var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
                if(json == null) throw new ApiRequestException("Null response");
                foreach(var e in (JsonArray)json["data"]) {
                    var ep = ToEpisode(e);
                    ret[(int)ep.Number] = ep;
                }
                next = json["links"].ContainsKey("next") ? json["links"]["next"] : null;
            } while(next != null);
            return ret;
        }

        /// <summary>
        /// Get list of episodes
        /// </summary>
        /// <param name="animeId"></param>
        /// <param name="offset">The episode to start at</param>
        /// <param name="list">The list of episodes to append to.</param>
        /// <returns>A 0 if empty response, the next offset, otherwise</returns>
        /// <remarks>This is a much better idea, and good for scrolling UIs.
        /// The episodes added are indexed by episode number.</remarks>
        public static int GetEpisodes(int animeId, int offset, ref List<AnimeEpisode> list) {
            var client = new HttpClient();
            var url = EpisodeItems + $"?filter%5BmediaId%5D={animeId}&page%5Blimit%5D=20&page%5Boffset%5D={offset}";
            var response = client.GetAsync(url).Result;
            if(response.StatusCode != HttpStatusCode.OK) {
                Debug.Write(response.Content.ReadAsStringAsync().Result, "Kitsu Episode WARNING");
                throw new ApiRequestException(response.StatusCode.ToString());
            }
            var json = JsonValue.Parse(response.Content.ReadAsStringAsync().Result);
            if(json == null) throw new ApiRequestException("Null response");
            if((JsonArray)json["data"].Count == 0) return 0;
            foreach(var e in (JsonArray)json["data"]) {
                var ep = ToEpisode(e);
                list[(int)ep.Number] = ep;
            }
            return offset + 20;
        }

        /// <summary>
        /// Update the anime on the authenticated user's list.
        /// </summary>
        /// <param name="a">The anime to update</param>
        /// <returns>True on success</returns>
        public async Task<bool> UpdateAnime(Anime a){
            AuthenticationCheck();
            
            // Not in list, remove it
            if(a.ListStatus == ApiEntry.ListStatuses.NotInList)
                return await RemoveAnime(a.Id);
            
            // Mark episode count as completed
            if(a.ListStatus == ApiEntry.ListStatuses.Completed)
                a.CurrentEpisode = a.Episodes;

            var id = await GetEntryId(a.Id, "anime");
            if(id == -2) return false; // Error
            if(id == -1) { // Not found
                await AddAnime(a.Id, a.ListStatus);
                id = await GetEntryId(a.Id, "anime");
                if(id < 0) return false;
            }

            var json = new JsonObject() {
                ["data"] = new JsonObject() {
                    ["id"] = id,
                    ["type"] = "libraryEntries",
                    ["attributes"] = new JsonObject() {
                        ["status"] = FromListStatus(a.ListStatus),
                        ["progress"] = a.CurrentEpisode,
                        ["notes"] = a.Notes,
                        ["startedAt"] = FromDateTime(a.UserStart),
                        ["finishedAt"] = FromDateTime(a.UserEnd)
                        
                    },
                    ["relationships"] = new JsonObject() {
                        ["user"] = new JsonObject() {
                            ["data"] = new JsonObject() {
                                ["id"] = _userId,
                                ["type"] = "users"
                            }
                        },
                        ["media"] = new JsonObject() {
                            ["data"] = new JsonObject() {
                                ["type"] = "anime",
                                ["id"] = a.Id
                            }
                        }
                    }
                }
            };
            // if we define it it must be bigger than 2.
            json["data"]["attributes"]["ratingTwenty"] = a.UserScore >= 1 ? (JsonValue)(a.UserScore * 2) : null;
            var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod("PATCH"), LibraryEntries + $"/{id}") {
                Content = new StringContent(json.ToString(), Encoding.UTF8, ContentType)
            });
            Debug.WriteIf(!response.IsSuccessStatusCode, response.Content.ReadAsStringAsync(), "Kitsu WARNING");
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get the authenticated user's anime list.
        /// </summary>
        /// <returns>A list of all anime in the user's list from the server.</returns>
        /// <exception cref="ApiRequestException">If a request fails.</exception>
        /// <remarks>Kitsu will only return 500 items at a time, so multiple requests may be sent.</remarks>
        public async Task<List<Anime>> PullAnimeList(){
            var ret = new List<Anime>();
            var url = LibraryEntries +
                         $"?filter%5Bkind%5D=anime\u0026filter%5Buser_id%5D={_userId}\u0026include=media\u0026page%5Blimit%5D=500\u0026page%5Boffset%5D=0";

            // Kitsu returns multiple pages, so we must do it page by page.
            // Luckily Kitsu will give us the URL to the next page!
            string next = null;
            do {
                if(next != null) url = next;
                var response = await _client.GetAsync(url);
                if(response.StatusCode != HttpStatusCode.OK) {
                    Debug.Write(response.Content.ReadAsStringAsync(), "Kitsu WARNING");
                    throw new ApiRequestException(response.StatusCode.ToString());
                }
                var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
                if(json == null) throw new ApiFormatException("Null response");
                
                foreach(var i in ((JsonArray)json["data"])) {
                    int id = i["relationships"]["media"]["data"]["id"];
                    var arr = ((JsonArray)json["included"]).FirstOrDefault(x => x["id"] == id);
                    if(arr == null) throw new ApiFormatException("Null JSON entry");
                    var a = ToAnime(arr);
                    a.ListStatus = ToListStatus(i["attributes"]["status"]);
                    a.CurrentEpisode = i["attributes"]["progress"];
                    a.Notes = i["attributes"]["notes"];
                    a.UserStart = ToDateTime(i["attributes"]["startedAt"]);
                    a.UserEnd = ToDateTime(i["attributes"]["finishedAt"]);
                    a.UserScore = (i["attributes"]["ratingTwenty"] ?? 0) / 2; 
                    ret.Add(a);
                }
                next = json["links"].ContainsKey("next") ? json["links"]["next"] : null;
            } while(next != null);
            return ret;
        }

        public async Task<bool> AddManga(int id){
            AuthenticationCheck();

            var data = new JsonObject() {
                ["data"] = new JsonObject {
                    ["type"] = "libraryEntries",
                    ["attributes"] = new JsonObject() {
                        ["status"] = FromListStatus(ApiEntry.ListStatuses.Current)
                    },
                    ["relationships"] = new JsonObject() {
                        ["user"] = new JsonObject() {
                            ["data"] = new JsonObject() {
                                ["id"] = _userId,
                                ["type"] = "users"
                            }
                        },
                        ["media"] = new JsonObject() {
                            ["data"] = new JsonObject() {
                                ["type"] = "manga",
                                ["id"] = id
                            }
                        }
                    },
                }
            };

            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, LibraryEntries) {
                Content = new StringContent(data.ToString(), Encoding.UTF8, ContentType),
            });
            Debug.WriteIf(!response.IsSuccessStatusCode, response.Content.ReadAsStringAsync(), "Kitsu WARNING");
            return response.StatusCode == HttpStatusCode.Created;
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
                            ["type"] = "manga",
                            ["id"] = id
                        }
                    }
                }
            };

            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, LibraryEntries) {
                Content = new StringContent(data.ToString(), Encoding.UTF8, ContentType),
            });
            Debug.WriteIf(!response.IsSuccessStatusCode, response.Content.ReadAsStringAsync(), "Kitsu WARNING");
            return response.StatusCode == HttpStatusCode.Created;
        }

        /// <summary>
        /// Remove the manga from the authenticated user's list
        /// </summary>
        /// <param name="id">The ID of the manga to remove.</param>
        /// <returns>True on success</returns>
        public async Task<bool> RemoveManga(int id){
            AuthenticationCheck();

            var entryId = await GetEntryId(id, "manga");
            if(entryId == -1) return true; // the entry wasn't there
            if(entryId == -2) return false; // something went wrong

            var response = await _client.DeleteAsync(LibraryEntries + $"/{entryId}");
            return response.IsSuccessStatusCode; //200OK, 204NOCONTENT
        }

        /// <summary>
        /// Search for a manga in the Kitsu database.
        /// </summary>
        /// <param name="keywords">The search term to use</param>
        /// <returns>A list of all results.</returns>
        /// <exception cref="ApiRequestException">If a request fails.</exception>
        /// <remarks>Kitsu will only return 20 items at a time, so multiple requests may be sent.</remarks>
        public async Task<List<Manga>> FindManga(string keywords){
            var ret = new List<Manga>(); // ?filter[text]=&page[limit]=20&page[offset]=0
            var url = MangaItems + $"?filter%5Btext%5D={keywords}\u0026page%5Blimit%5D=20&page%5Boffset%5D=0";
            string next = null;
            var count = 0;
            do {
                if(next != null) url = next;
                var response = await _client.GetAsync(url);
                if(response.StatusCode != HttpStatusCode.OK) {
                    Debug.Write(response.Content.ReadAsStringAsync(), "Kitsu WARNING");
                    throw new ApiRequestException(response.StatusCode.ToString());
                }
                var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
                if(json == null) throw new ApiRequestException("Null Response");
                ret.AddRange(from m in (JsonArray)json["data"] select ToManga(m));
                next = json["links"].ContainsKey("next") ? json["links"]["next"] : null; // get the next page if there is one
                count++;
            } while(count <= 5 && next != null);
            return ret;
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

            var id = await GetEntryId(m.Id, "manga");
            if(id == -2) return false;
            if(id == -1) {
                await AddManga(m.Id, m.ListStatus);
                id = await GetEntryId(m.Id, "manga");
                if(id < 0) return false;
            }

            var json = new JsonObject() {
                ["data"] = new JsonObject() {
                    ["id"] = id,
                    ["type"] = "libraryEntries",
                    ["attributes"] = new JsonObject() {
                        ["status"] = FromListStatus(m.ListStatus),
                        ["progress"] = m.CurrentChapter,
                        ["volumesOwned"] = m.CurrentVolume,
                        ["notes"] = m.Notes,
                        ["startedAt"] = FromDateTime(m.UserStart),
                        ["finishedAt"] = FromDateTime(m.UserEnd),
                    },
                    ["relationships"] = new JsonObject() {
                        ["user"] = new JsonObject() {
                            ["data"] = new JsonObject() {
                                ["id"] = _userId,
                                ["type"] = "users"
                            }
                        },
                        ["media"] = new JsonObject() {
                            ["data"] = new JsonObject() {
                                ["type"] = "anime",
                                ["id"] = m.Id
                            }
                        }
                    }
                }
            };
            // if we define it, it must be bigger thna 2.
            json["data"]["attributes"]["ratingTwenty"] = m.UserScore >= 1 ? (JsonValue)(m.UserScore * 2) : null;
            var response = await _client.SendAsync(
                new HttpRequestMessage(new HttpMethod("PATCH"), LibraryEntries + $"/{id}") {
                    Content = new StringContent(json.ToString(), Encoding.UTF8, ContentType)
                });
            Debug.WriteIf(!response.IsSuccessStatusCode, response.Content.ReadAsStringAsync(), "Kitsu WARNING");
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get the authenticated user's anime list.
        /// </summary>
        /// <returns>A list of all anime in the user's list from the server.</returns>
        /// <exception cref="ApiRequestException">If a request fails.</exception>
        /// <remarks>Kitsu will only return 500 items at a time, so multiple requests may be sent.</remarks>
        public async Task<List<Manga>> PullMangaList(){
            var ret = new List<Manga>();
            var url = LibraryEntries +
                      $"?filter%5Bkind%5D=manga\u0026filter%5Buser_id%5D={_userId}\u0026include=media\u0026page%5Blimit%5D=500\u0026page%5Boffset%5D=0";

            string next = null;
            do {
                if(next != null) url = next;
                var response = await _client.GetAsync(url);
                if(response.StatusCode != HttpStatusCode.OK) {
                    Debug.Write(response.Content.ReadAsStringAsync(), "Kitsu WARNING");
                    throw new ApiRequestException(response.StatusCode.ToString());
                }
                var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
                if(json == null) throw new ApiFormatException("Null response");

                foreach(var i in ((JsonArray)json["data"])) {
                    int id = i["relationships"]["media"]["data"]["id"];
                    var media = ((JsonArray)json["included"]).FirstOrDefault(x => x["id"] == id);
                    var attr = (JsonObject)i["attributes"];
                    if(media == null) throw new ApiFormatException("Null JSON entry");
                    var m = ToManga(media);
                    m.ListStatus = ToListStatus(attr["status"]);
                    m.CurrentChapter = attr["progress"];
                    m.CurrentVolume = attr["volumesOwned"];
                    m.Notes = attr["notes"];
                    m.UserStart = ToDateTime(attr["startedAt"]);
                    m.UserEnd = ToDateTime(attr["finishedAt"]);
                    ret.Add(m);
                }
                next = json["links"].ContainsKey("next") ? json["links"]["next"] : null;
            } while(next != null);
            return ret;
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
                    Debug.Fail($"Invalid list status '{status}'");
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
                    Debug.Fail($"Invalid list status '{status}'");
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
                    Debug.Fail($"Invalid show type '{type}'");
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
                case "manhwa": 
                    return Manga.MangaTypes.Manhwa;
                case "oel":
                    return Manga.MangaTypes.Comic;
                default:
                    Debug.Fail($"Invalid manga type '{type}'");
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        // On Kitsu, each item has a unique entry id. For some operations, we need it.
        // type is either 'anime', 'manga', or 'drama'
        // -2 = Response failed
        // -1 = Entry does not exist
        private async Task<int> GetEntryId(int entry, string type) {
            var response = await _client.GetAsync(
                // ?filter[user_id]=&filter[kind]=&filter[_id]=
                LibraryEntries + $"?filter%5Buser_id%5D={_userId}\u0026filter%5Bkind%5D={type}\u0026filter%5B{type}_id%5D={entry}");
            if(response.StatusCode != HttpStatusCode.OK) return -2;
            var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
            if(json == null) throw new ApiRequestException("Null JSON");
            if(json["data"].Count == 0) return -1;
            return json["data"][0]["id"];
        }

        // TODO: Airtimes (possibly available through this api)
        private static Anime ToAnime(JsonValue json) {
            if(json == null) return null;
            Debug.Assert(json["type"] == "anime");
            var attr = json["attributes"];
            
            int id = json["id"];
            string[] synonyms; // sometimes this is null
            if(!attr.ContainsKey("abbreviatedTitles") || attr["abbreviatedTitles"] == null)
                synonyms = new string[0];
            else {
                synonyms = new string[attr["abbreviatedTitles"].Count];
                for(var i = 0; i < attr["abbreviatedTitles"].Count; i++)
                    synonyms[i] = attr["abbreviatedTitles"][i];
            }
            
            var score = attr["averageRating"] == null ? 0.0 : (double)attr["averageRating"];
            string title = attr["canonicalTitle"];
            var enTitle = attr["titles"].ContainsKey("en") ? (string)attr["titles"]["en"] : title;
            var romajiTitle = attr["titles"].ContainsKey("en_jp") ? (string)attr["titles"]["en_jp"] : title;
            var jpTitle = attr["titles"].ContainsKey("ja_jp") ? (string)attr["titles"]["ja_jp"] : title;
            string image = attr["posterImage"]?["original"];
            var start = ToDateTime(attr["startDate"]);
            var end = ToDateTime(attr["endDate"]);
            int episodes = attr["episodeCount"] ?? 0;
            var type = ToShowType(attr["showType"]);
            Anime.RunningStatuses status;
            if(start == DateTime.MinValue) status = Anime.RunningStatuses.NotYetAired;
            else if(end == DateTime.MinValue) status = Anime.RunningStatuses.Airing;
            else status = Anime.RunningStatuses.Completed;
            string synopsis = attr["synopsis"];
            return new Anime(id, romajiTitle, enTitle, jpTitle, synonyms, episodes, null, score, type, status, start,
                end, synopsis, image, Identifier);
        }

        private static Manga ToManga(JsonValue json) {
            if(json == null) return null;
            if(json["type"] != "manga") 
                throw new ApiFormatException("Unexpected type encountered");
            var attr = json["attributes"];

            int id = json["id"];
            string[] synonyms;
            if(!attr.ContainsKey("abbreviatedTitles") || attr["abbreviatedTitles"] == null)
                synonyms = new string[0];
            else {
                synonyms = new string[attr["abbreviatedTitles"].Count];
                for(var i = 0; i < attr["abbreviatedTitles"].Count; i++)
                    synonyms[i] = attr["abbreviatedTitles"][i];
            }

            var score = attr["averageRating"] == null ? 0.0 : (double)attr["averageRating"];
            string title = attr["canonicalTitle"];
            var enTitle = attr["titles"].ContainsKey("en") ? (string)attr["titles"]["en"] : title;
            var romajiTitle = attr["titles"].ContainsKey("en_jp") ? (string)attr["titles"]["en_jp"] : title;
            var jpTitle = attr["titles"].ContainsKey("ja_jp") ? (string)attr["titles"]["ja_jp"] : title;
            string image = attr["posterImage"]?["original"];
            var start = ToDateTime(attr["startDate"]);
            var end = ToDateTime(attr["endDate"]);
            int chapters = attr["chapterCount"] ?? 0;
            int volumes = attr["volumeCount"] ?? 0;
            var type = ToMangaType(attr["mangaType"]);
            Manga.RunningStatuses status;
            if(start == DateTime.MinValue) status = Manga.RunningStatuses.NotYetPublished;
            else if(end == DateTime.MinValue) status = Manga.RunningStatuses.Publishing;
            else status = Manga.RunningStatuses.Finished;
            string synopsis = attr["synopsis"];
            return new Manga(id, romajiTitle, enTitle, jpTitle, synonyms, chapters, volumes, score, type, status, start,
                end, synopsis, image);
        }

        private static AnimeEpisode ToEpisode(JsonValue json) {
            if(json == null) return null;
            var eng = json["attributes"]["titles"].ContainsKey("en_us") ? (string)json["attributes"]["titles"]["en_us"] : (string)json["attributes"]["canonicalTitle"];
            var jp = json["attributes"]["titles"].ContainsKey("ja_jp") ? (string)json["attributes"]["titles"]["ja_jp"] : (string)json["attributes"]["canonicalTitle"];
            return new AnimeEpisode(ToDateTime(json["attributes"]["airdate"]), json["attributes"]["number"], json["attributes"]["season"] ?? 0, eng, jp, json["attributes"]["synopsis"]);
        }

        private static DateTime ToDateTime(string date){
            if(date == null) return DateTime.MinValue;
            DateTime ret;
            if(DateTime.TryParseExact(date, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out ret))
                return ret;
            throw new ApiFormatException("Could not parse datetime " + date);
        }

        private static string FromDateTime(DateTime dt) {
            return dt == DateTime.MinValue ? null : 
                dt.ToString(DateFormat[1], CultureInfo.InvariantCulture);
        }

        // when performing oauth functions, lets make sure that our token has not expired.
        private async void AuthenticationCheck(){
            if(_userId == 0 || !await VerifyCredentials())
                throw new ApiRequestException("[Kitsu] Could not verify user credentials");
            if(DateTime.Now >= _expiration) await Authenticate();
        }

        private static void GetClientInfo() {
            using(var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Trackr.Api.Resources.kitsu.json")) {
                if(s == null) throw new ApiRequestException("Client data not found");
                using(var r = new StreamReader(s)) {
                    var json = (JsonObject)JsonValue.Parse(r.ReadToEnd());
                    _clientId = json?["id"];
                    _clientSecret = json?["secret"];
                }
            }
        }
        
        ~Kitsu(){
            _client.Dispose();
        }
    }
}