﻿﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Json;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
  
 using Trackr.Core;

namespace Trackr.Api {
	public class AniList : Api, IAnime, IManga {
		public const string Identifier = "AniList";
		/// <summary>
		/// The name of the current API
		/// </summary>
		public override string Name { get; }  = Identifier;

		/// <summary>
		/// The username of the account signed in to AniList.
		/// </summary>
		public override string Username {
			get {
				if (_credentials.Username == null) FetchUsername().Wait();
				return _credentials.Username;
			}
		}

		private const string UrlBase = "https://graphql.anilist.co";
		private const string OAuth = "https://anilist.co/api/v2/oauth/";
		//private const string PinUrl = "https://anilist.co/api/v2/oauth/pin";
		private static string _clientId;
		private static string ClientId {
			get { if(_clientId == null) GetClientInfo(); // pull json data
				return _clientId;
			}
		}
		private static string _clientSecret;
		private static string ClientSecret {
			get { if(_clientSecret == null) GetClientInfo(); // pull json data
				return _clientSecret;
			}
		}
		private const string ContentType = "application/json";

		private readonly HttpClient _client;
		private readonly UserPass _credentials; // The password will be the authorization code
		private DateTime _expiration;
		private int _userId;
		private Account _account;

		/// <summary>
		/// This event is raised if the token has expired (older than a year)
		/// </summary>
		/// <remarks>When the token has expired, a new authentication token must be retrieved from the server.
		/// The user is responsible for signing in and copying the authentication token into the client. The client should
		/// then update the token using ReceiveAuthToken() and then exchange the token for an access token.</remarks>
		public event EventHandler TokenExpired;
		
		/// <summary>
		/// The URL to take the user to in order to authorize Trackr.
		/// </summary>
		public static string RedirectUrl => OAuth + $"authorize?client_id={ClientId}&response_type=code";

		
		/// <summary>
		/// Create a new instance of the AniList API.
		/// </summary>
		/// <param name="a">The account information to use</param>
		/// <remarks>We are using a refernece to the account because we need to actively keep track of the expiration date of the token.
		/// Note as well that the username component will be null if we have an authentication token, and defined if it is an access token.</remarks>
		public AniList(Account a) {
			_account = a;
			_credentials = a.Credentials;
			_expiration = a.Expiration;
			_client = new HttpClient();
		}

		// If we have an auth code, we must trade it for an access token
		private async Task<bool> FetchToken() {
			var data = new FormUrlEncodedContent(new [] {
				new KeyValuePair<string, string>("grant_type", "authorization_code"),
				new KeyValuePair<string, string>("client_id", ClientId),
				new KeyValuePair<string, string>("client_secret", ClientSecret),
				new KeyValuePair<string, string>("code", _credentials.Password), 
			});
			var response = await _client.PostAsync(OAuth + "token", data);
			Debug.WriteIf(!response.IsSuccessStatusCode, response.Content.ReadAsStringAsync().Result + $"({response.StatusCode}");
			var json = (JsonObject)JsonValue.Parse(await response.Content.ReadAsStringAsync());
			if(response.StatusCode != HttpStatusCode.OK) throw new ApiRequestException(json?["message"].ToString() ?? response.StatusCode.ToString());
			if(json?["access_token"] == null) throw new ApiRequestException("Null response");
			_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(json["token_type"], json["access_token"]);
			_credentials.Password = json["access_token"]; // store the new access token
			_expiration = DateTime.Now.AddSeconds(json["expires_in"]);
			_account.Expiration = _expiration;
			return true;
		}

		/// <summary>
		/// This function must be called when and only when AniList invokes the TokenExpired event.
		/// Calling this function will update the saved token in order to trade it for an access token.
		/// </summary>
		/// <param name="token">The new authentication token</param>
		public void SendAuthToken(string token) {
			_credentials.Password = token;
		}

		private bool ReceiveAuthToken() {
			// If the token expiration event is null, it will cause problems; we leave an exception for if this is the first time we are getting the data (add account screen)
			if(TokenExpired == null) throw new ApiRequestException("[AniList] The token event handler has not been properly connected to handle it!");
			var token = _credentials.Password;
			TokenExpired.Invoke(this, EventArgs.Empty); // this should block the thread until the user is done entering the pin
			if(_credentials.Password == string.Empty) { // user decided not to update it!
				_credentials.Password = token;
				return false;
			}
			return true;
		}
		
		// Check if we still have an authentication token that needs to be exchanged, of if our access token has expired
		private async Task<bool> AuthenticationCheck() {
			// We have an authorization token.. we want an access token!
			if(_credentials.Username == null) {
				if(_credentials.Password == null)
					if(!ReceiveAuthToken()) return false;
				await FetchToken();
				await FetchUsername();
			}
			// We have an access token, but it expired...
			else if(_expiration <= DateTime.Now) {
				if(!ReceiveAuthToken()) return false;
				await FetchToken();
				await FetchUsername();
			}

			// We've authorized the wrong account! This may corrupt the lists!
			// if it is null, this is the first time we're getting the data!
			if(_account.Username != null && _credentials.Username != _account.Username) {
				_credentials.Username = null;
				_credentials.Password = string.Empty;
				throw new ApiRequestException($"[AniList] The provided authorization token has attempted to authorize " +
				                              $"with the wrong account! Please use {_account.Username}." +
				                              $"If the username has changed recently, please delete the account in " +
				                              $"settings and add it again.");
			}
			return true;
		}
		
		// grab the username from the server
		private async Task FetchUsername() {
			
			const string q = @"
				{
  					Viewer{
    					id
    					name
  					}
				}";
			var req = new JsonObject() { ["query"] = q };
			var response = _client.PostAsync(UrlBase, new StringContent(req.ToString(), Encoding.UTF8, ContentType)).Result;
			Debug.WriteLineIf(!response.IsSuccessStatusCode, response.Content.ReadAsStringAsync().Result, "AniList Username acquisition WARNING");
			if(!response.IsSuccessStatusCode) throw new ApiRequestException(response.Content.ReadAsStringAsync().Result);
			var json = (JsonObject)JsonValue.Parse(await response.Content.ReadAsStringAsync());
			if(json == null) throw new ApiFormatException("Null response");
			_credentials.Username = json["data"]["Viewer"]["name"];
			_userId = json["data"]["Viewer"]["id"];
		}

		/// <summary>
		/// Running this function authenticates the user with AniList's OAuth2 service.
		/// </summary>
		/// <remarks>Authentication will be required once the access token expires - handle with the TokenExpired event.</remarks>
		/// <returns>True on success</returns>
		public override async Task<bool> VerifyCredentials() {
			await AuthenticationCheck();
			return Username != null;
		}
			
		/// <summary>
		/// Add an anime to the anime list
		/// </summary>
		/// <param name="id">The AniList media ID</param>
		/// <param name="listStatus">The list status of the anime</param>
		/// <returns>True on success</returns>
		public async Task<bool> AddAnime(int id, ApiEntry.ListStatuses listStatus) {
			const string q = @"
				{
					mutation($id: Int, $status: MediaListStatus) {
						SaveMediaListEntry (mediaId: $id, status: $status) {
							mediaId
					}
				}
			";

			var v = new JsonObject() {
				["id"] = id,
				["status"] = FromListStatus(listStatus)
			};
			var json = await SendRequest(q, v);
			return id == json?["data"]?["SaveMediaListEntry"]["mediaId"];
		}
		
		/// <summary>
		/// Add an anime to the anime list
		/// </summary>
		/// <param name="id">The AniList media ID</param>
		/// <returns>True on success</returns>
		public async Task<bool> AddAnime(int id) {
			const string q = @"
				{
					mutation($id: Int, $status: MediaListStatus) {
						SaveMediaListEntry (mediaId: $id, status: $status) {
							mediaId
					}
				}
			";

			var v = new JsonObject() {
				["id"] = id,
				["status"] = FromListStatus(ApiEntry.ListStatuses.Current)
			};

			var json = await SendRequest(q, v);
			return id == json?["data"]?["SaveMediaListEntry"]["mediaId"];
		}

		/// <summary>
		/// Remove an anime from the anime list.
		/// </summary>
		/// <param name="id">The AniList media ID</param>
		/// <returns>True on success</returns>
		public async Task<bool> RemoveAnime(int id) {
			var getId = GetEntryId(id);

			const string q = @"
				{
					mutation($id: Int) {
						DeleteMediaListEntry(id: $id) {
							deleted
						}
				}
			";
			var entryId = await getId;
			if(id == -1) {
				Debug.WriteLine("Entry already removed", "AniList Remove WARNING");
				return true;
			}
			var v = new JsonObject() {
				["id"] = entryId
			};
			var json = await SendRequest(q, v);
			return json["data"]["DeleteMediaListEntry"]["deleted"];
		}

		/// <summary>
		/// Search for an anime in the AniList database
		/// </summary>
		/// <param name="keywords">The search terms to use</param>
		/// <returns>A list of results</returns>
		public async Task<List<Anime>> FindAnime(string keywords) {
			const string q = @"
				{
					query($keywords: String, $page: Int, $per: Int){
						Page(page: $page, perPage: $per){
							pageInfo{
								hasNextPage
							}
 
							Media(search: $keywords, type: ANIME){
								id
								title {
									romaji
									english
									native
								}
								format
								MediaStatus
								description
								startDate
								endDate
								episodes
								coverImage
								synonyms
								averageScore
							}
						}
					}
				}
			";
			var page = 1;
			var ret = new List<Anime>();
			JsonValue json;
			do {
				var v = new JsonObject() {
					["keywords"] = keywords,
					["page"] = page,
					["per"] = 50
				};
				json = await SendRequest(q, v);
				ret.AddRange(from a in (JsonArray)json["data"]["media"] select ToAnime(a));
				page++;
			} while(json["data"]["Page"]["pageInfo"]["hasNextPage"] && page < 3);
			return ret;
		}

		/// <summary>
		/// Update an anime from the list.
		/// </summary>
		/// <param name="anime">The anime to update, with updated values</param>
		/// <returns>True on success</returns>
		public async Task<bool> UpdateAnime(Anime anime) {
			var id = await GetEntryId(anime.Id);
			if(id == -1) {
				Debug.WriteLine("Anime not in database", "AniList update WARNING");
				return false;
			}

			const string q = @"
				{
					mutation($id: Int, $status: MediaListStatus, $score: Float, $progress: Int, $notes: String, $start: FuzzyDateInput, $end: FuzzyDateInput) {
						SaveMediaListEntry(id: $id, status: $status, score(POINT_10): $score, progress: $progress, notes: $notes, startedAt: $start, completedAt: $end){
							id
							mediaId
						}
					}
				}
			";

			var v = new JsonObject() {
				["id"] = id,
				["status"] = FromListStatus(anime.ListStatus),
				["score"] = anime.UserScore,
				["progress"] = anime.CurrentEpisode,
				["notes"] = anime.Notes,
				["start"] = FromDateTime(anime.UserStart),
				["end"] = FromDateTime(anime.UserEnd)
			};

			var json = await SendRequest(q, v);
			Debug.WriteLineIf(
				id != json?["data"]["SaveMediaListEntry"]["id"] || anime.Id != json["data"]["SaveMediaListEntry"]["mediaId"],
				"The parameters returned don't match!", "AniList UpdateAnime WARNING");
			return true;
		}

		/// <summary>
		/// Pulls a copy of the anime list from the server.
		/// </summary>
		/// <returns>The list of anime on the server's copy of the list.</returns>
		public async Task<List<Anime>> PullAnimeList() {
			var ret = new List<Anime>();
			JsonValue json;

			const string q = @"
				{
					query($pg : Int, $per: Int, $id: Int) {
						Page(page: $pg, perPage: $per) {
							pageInfo {
								hasNextPage
							}
							
							MediaList(userId: $id, type: ANIME) {
								status
								score(POINT_10)
								progress
								notes
								startedAt
								completedAt
								media
							}
						}
					}
				}
			";
			var pg = 1;
			do {
				var v = new JsonObject() {
						["pg"] = pg,
						["per"] = 50,
						["id"] = _userId
				};
				json = await SendRequest(q, v);

				foreach(JsonObject x in json["data"]["Page"]["MediaList"]) {
					var a = ToAnime(x["media"]);
					a.ListStatus = ToListStatus(x["status"]);
					a.UserScore = x["score"] ?? 0;
					a.CurrentEpisode = x["progress"] ?? 0;
					a.Notes = x["notes"];
					a.UserStart = ToDateTime(x["startedAt"]);
					a.UserEnd = ToDateTime(x["completedAt"]);
					ret.Add(a);
				}
				pg++;
			} while(json["data"]["Page"]["pageInfo"]["hasNextPage"]);
			return ret;
		}

		/// <summary>
		/// Add a manga to the list.
		/// </summary>
		/// <param name="id">The AniList media ID</param>
		/// <param name="listStatus">The manga list status</param>
		/// <returns>True on success</returns>
		public async Task<bool> AddManga(int id, ApiEntry.ListStatuses listStatus) {
			return await AddAnime(id, listStatus); // Exact same request, anime and manga ids are unique!
		}

		/// <summary>
		/// Add a manga to the list.
		/// </summary>
		/// <param name="id">The AniList media ID</param>
		/// <returns>True on success</returns>
		public async Task<bool> AddManga(int id) {
			return await AddAnime(id);
		}
		
		/// <summary>
		/// Remove a manga from the list.
		/// </summary>
		/// <param name="id">The AniList media ID</param>
		/// <returns>True on success</returns>
		public async Task<bool> RemoveManga(int id) {
			return await RemoveAnime(id);
		}

		/// <summary>
		/// Search for a manga in the AniList database.
		/// </summary>
		/// <param name="keywords">The search terms to use</param>
		/// <returns>A list of search results</returns>
		public async Task<List<Manga>> FindManga(string keywords) {
			const string q = @"
				{
					query($keywords: String, $page: Int, $per: Int){
						Page(page: $page, perPage: $per){
							pageInfo{
								hasNextPage
							}
						
						Media(search: $keywords, type: MANGA){
							id
							title {
								romaji
								english
								native
							}
							format
							mediaStatus
							description
							startDate
							endDate
							chapters
							volumes
							coverInage
							synonyms
							averageScore
						}
					}
				}
			";
			var page = 1;
			var ret = new List<Manga>();
			JsonValue json;
			do {
				var v = new JsonObject() {
					["keywords"] = keywords,
					["page"] = page,
					["per"] = 50
				};
				json = await SendRequest(q, v);
				ret.AddRange(from m in (JsonArray)json["data"]["media"] select ToManga(m));
				page++;
			} while(json["data"]["Page"]["pageInfo"]["hasNextPage"] && page < 3);
			return ret;
		}
		
		/// <summary>
		/// Update a manga on the list
		/// </summary>
		/// <param name="manga">The manga to update with updated values</param>
		/// <returns>True on success</returns>
		public async Task<bool> UpdateManga(Manga manga) {
			var id = await GetEntryId(manga.Id);
			if(id == -1) {
				Debug.WriteLine("Manga not in database", "Manga update WARNING");
				return false;
			}

			const string q = @"
				{
					mutation($id: Int, $status: MediaListStatus, $score: Float, $ch: Int, $vol: Int, $notes: String, $start: FuzzyDateInput, $end: FuzzyDateInput) {
						  SaveMediaListEntry(id: $id, status: $status, score(POINT_10): $score, progress: $ch, progressVolumes: $vol, notes: $notes, startedAt: $start, completedAt: $end) {
							id
							mediaId
						}
					}
				}			
			";

			var v = new JsonObject() {
				["id"] = id,
				["status"] = FromListStatus(manga.ListStatus),
				["score"] = manga.UserScore,
				["ch"] = manga.CurrentChapter,
				["vol"] = manga.CurrentVolume,
				["notes"] = manga.Notes,
				["start"] = FromDateTime(manga.UserStart),
				["end"] = FromDateTime(manga.UserEnd)
			};

			var json = await SendRequest(q, v);
			Debug.WriteLineIf(
				id != json?["data"]["SaveMediaListEntry"]["id"] || manga.Id != json["data"]["SaveMediaListEntry"]["mediaId"],
				"The parameters returned don't match!", "AniList UpdateManga WARNING");
			return true;
		}

		/// <summary>
		/// Pulls a copy of the manga list from the server
		/// </summary>
		/// <returns>A list of manga from the server's copy of the list.</returns>
		public async Task<List<Manga>> PullMangaList() {
			var ret = new List<Manga>();
			JsonValue json;

			const string q = @"
				{
					query($pg: Int, $per: Int, $id: Int) {
						Page(page: $pg, perPage: $per) {
							pageInfo {
								hasNextPage
							}

							MediaList(userId: $id, type: MANGA) {
								status
								score(POINT_10)
								progress
								progressVolumes
								notes
								startedAt
								completedAt
								media
							}
						}
					}
				}
			";
			var pg = 1;
			do {
				var v = new JsonObject() {
						["pg"] = pg,
						["per"] = 50,
						["id"] = _userId
				};
				json = await SendRequest(q, v);

				foreach(JsonObject x in json["data"]["Page"]["MediaList"]) {
					var m = ToManga(x["media"]);
					m.ListStatus = ToListStatus(x["status"]);
					m.UserScore = x["score"] ?? 0;
					m.CurrentChapter = x["progress"] ?? 0;
					m.CurrentVolume = x["progressVolumes"] ?? 0;
					m.Notes = x["notes"];
					m.UserStart = ToDateTime(x["startedAt"]);
					m.UserEnd = ToDateTime(x["completedat"]);
					ret.Add(m);
				}
				pg++;
			} while(json["data"]["Page"]["pageInfo"]["hasNextPage"]);
			return ret;
		}

		private async Task<int> GetEntryId(int id) {
			await AuthenticationCheck();
			
			const string q = @"
				query($userId: Int, $mediaId: Int) {
					MediaList(userId: $userd, $mediaId: $mediaId) {
						id
					}
				}
			";
			var req = new JsonObject() {
				["query"] = q,
				["variables"] = new JsonObject() {
					["userId"] = _userId,
					["mediaId"] = id
				}
			};

			var response = await _client.PostAsync(UrlBase, new StringContent(req, Encoding.UTF8, ContentType));
			var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
			if(!response.IsSuccessStatusCode) {
				Debug.WriteLine(json?["errors"]?["message"] ?? response.StatusCode.ToString(), "AniList GetEntryId WARNING");
				return -2;
			}
			if(json == null) throw new ApiRequestException("Null JSON");
			if(json["data"]["MediaList"].Count == 0) return -1;
			return json["data"]["MediaList"]["id"];
		}

		private static string FromListStatus(ApiEntry.ListStatuses status) {
			switch(status) {
					case ApiEntry.ListStatuses.Current:
						return "CURRENT";
					case ApiEntry.ListStatuses.Planned:
						return "PLANNING";
					case ApiEntry.ListStatuses.Completed:
						return "COMPLETED";
					case ApiEntry.ListStatuses.Dropped:
						return "DROPPED";
					case ApiEntry.ListStatuses.OnHold:
						return "PAUSED";
					default:
						Debug.WriteLine("Invalid list status encountered: NotInList!", "AniList ListStatus WARNING");
						throw new InvalidEnumArgumentException(nameof(status), (int)status, typeof(ApiEntry.ListStatuses));
			}
		}

		private static ApiEntry.ListStatuses ToListStatus(string status) {
			switch(status) {
					case "CURRENT": case "REPEATING":
						return ApiEntry.ListStatuses.Current;
					case "PLANNING":
						return ApiEntry.ListStatuses.Planned;
					case "COMPLETED":
						return ApiEntry.ListStatuses.Completed;
					case "DROPPED":
						return ApiEntry.ListStatuses.Dropped;
					case "PAUSED":
						return ApiEntry.ListStatuses.OnHold;
					default:
						Debug.WriteLine($"Invalid list status encountered: {status}", "AniList ListStatus WARNING");
						throw new InvalidEnumArgumentException("Invalid list status type encountered");
			}
		}

		private static Anime.RunningStatuses ToRunningStatus(string status) {
			switch(status) {
					case "FINISHED":
						return Anime.RunningStatuses.Completed;
					case "RELEASING":
						return Anime.RunningStatuses.Airing;
					case "NOT_YET_AIRED": case "CANCELLED":
						return Anime.RunningStatuses.NotYetAired;
				default:
					Debug.WriteLine($"Invalid running status encountered: {status}", "AniList RunningStatus WARNING");
					throw new InvalidEnumArgumentException("Invalid running status type encountered");
			}
		}

		private static Manga.RunningStatuses ToMangaStatus(string status) {
			switch(status) {
				case "FINISHED":
					return Manga.RunningStatuses.Finished;
				case "RELEASING":
					return Manga.RunningStatuses.Publishing;
				case "NOT_YET_AIRED": case "CANCELLED":
					return Manga.RunningStatuses.NotYetPublished;
				default:
					Debug.WriteLine($"Invalid running status encountered: {status}", "AniList RunningStatus WARNING");
					throw new InvalidEnumArgumentException("Invalid running status type encountered");
			}
		}

		private static Anime.ShowTypes ToShowType(string type) {
			switch(type) {
					case "TV": case "TV_SHORT":
						return Anime.ShowTypes.Tv;
					case "MOVIE":
						return Anime.ShowTypes.Movie;
					case "SPECIAL":
						return Anime.ShowTypes.Special;
					case "OVA":
						return Anime.ShowTypes.Ova;
					case "ONA":
						return Anime.ShowTypes.Ona;
					case "MUSIC":
						return Anime.ShowTypes.Music;
					default:
						Debug.WriteLine($"Invalid show type encountered: {type}", "AniList ShowType WARNING");
						throw new InvalidEnumArgumentException("Invalid show type encountered");
			}
		}

		private static Manga.MangaTypes ToMangaType(string type) {
			switch(type) {
					case "MANGA":
						return Manga.MangaTypes.Manga;
					case "NOVEL":
						return Manga.MangaTypes.Novel;
					case "ONE_SHOT":
						return Manga.MangaTypes.OneShot;
					default:
						Debug.WriteLine($"Invalid manga type encountered: {type}", "AniList MangaType WARNING");
						throw new InvalidEnumArgumentException("Invalid manga type encountered");
			}
		}

		private static DateTime ToDateTime(JsonValue d) {
			if(d["year"] == null && d["month"] == null && d["day"] == null) return DateTime.MinValue;
			return new DateTime(d["year"], d["month"], d["day"]);
		}

		private static JsonObject FromDateTime(DateTime d) {
			if(d == DateTime.MinValue)
				return new JsonObject() {
					["year"] = null,
					["month"] = null,
					["day"] = null
				};
			return new JsonObject() {
				["year"] = d.Year,
				["month"] = d.Month,
				["day"] = d.Day
			};
		}

		private static Anime ToAnime(JsonValue a) {
			int id = a["id"];
			string romaji = a["title"]["romaji"];
			string english = a["title"]["english"] ?? romaji;
			string native = a["title"]["native"] ?? romaji;
			var type = ToShowType(a["format"]);
			var status = ToRunningStatus(a["MediaStatus"]);
			string synopsis = a["description"];
			var start = ToDateTime(a["startDate"]);
			var end = ToDateTime(a["endDate"]);
			int episodes = a["episodes"];
			string image = a["coverImage"]["large"];
			var synonyms = new string[a["synonyms"].Count];
			for(var i = 0; i < synonyms.Length; i++)
				synonyms[i] = a["synonyms"][i];
			double score = a["averageScore"];
			return new Anime(id, romaji, english, native, synonyms, episodes, null, score, type, status, start, end, synopsis,
				image, Identifier);
		}

		private static Manga ToManga(JsonValue m) {
			int id = m["id"];
			string romaji = m["title"]["romaji"];
			string english = m["title"]["english"] ?? romaji;
			string native = m["title"]["native"] ?? romaji;
			var type = ToMangaType(m["format"]);
			var status = ToMangaStatus(m["MediaStatus"]);
			string synopsis = m["description"];
			var start = ToDateTime(m["startDate"]);
			var end = ToDateTime(m["endDate"]);
			int chapters = m["chapters"];
			int volumes = m["volumes"];
			string image = m["coverImage"]["large"];
			var synonyms = new string[m["synonyms"].Count];
			for(var i = 0; i < synonyms.Length; i++)
				synonyms[i] = m["synonyms"][i];
			double score = m["averageScore"];
			return new Manga(id, romaji, english, native, synonyms, chapters, volumes, score, type, status, start, end, synopsis,
				image);
		}
		
		// Pulls client information from the embedded json file
		private static void GetClientInfo() {
			using(var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Trackr.Api.Resources.anilist.json")) {
				if(s == null) throw new ApiRequestException("Client data not found");
				using(var r = new StreamReader(s)) {
					var json = (JsonObject)JsonValue.Parse(r.ReadToEnd());
					_clientId = json?["id"];
					_clientSecret = json?["secret"];
				}
			}
		}

		// Send an API request. This also implicitly checks the access token status.
		private async Task<JsonValue> SendRequest(string query, JsonValue variables) {
			Task auth = AuthenticationCheck();
			
			var req = new JsonObject() {
				["query"] = query,
				["variables"] = variables
			};
			await auth;
			var response = await _client.PostAsync(UrlBase, new StringContent(req.ToString(), Encoding.UTF8, ContentType));
			var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
			if(json == null) throw new ApiRequestException("Null JSON value");
			if(!response.IsSuccessStatusCode) {
				Debug.WriteLine(json["errors"] ?? response.StatusCode.ToString(), "AniList WARNING");
				throw new ApiRequestException(json["errors"]?["message"] ?? response.StatusCode.ToString());
			}
			return json;
		}

		~AniList() {
			_client.Dispose();
		}
	}
}