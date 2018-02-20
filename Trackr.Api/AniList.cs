using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Json;
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
		public override string Username => _credentials.Username;
		
		private const string UrlBase = "https://graphql.anilist.co";
		private const string OAuth = "https://anilist.co/api/v2/oauth/";
		//private const string PinUrl = "https://anilist.co/api/v2/oauth/pin";
		private static string _clientId;
		private static string ClientId {
			get { if(_clientId == null) GetClientInfo();
				return _clientId;
			}
		}
		private static string _clientSecret;
		private static string ClientSecret {
			get { if(_clientSecret == null) GetClientInfo();
				return _clientSecret;
			}
		}
		private const string ContentType = "application/json";

		private readonly HttpClient _client;
		private readonly UserPass _credentials; // The password will be the authorization code
		private DateTime _expiration;
		private int _userId;
		
		/// <summary>
		/// The URL to take the user to in order to authorize Trackr.
		/// </summary>
		public static string RedirectUrl => OAuth + $"authorize?client_id={ClientId}&response_type=code";


		public AniList(UserPass credentials) {
			_credentials = credentials;
			_expiration = DateTime.Now;
			_client = new HttpClient();
			_credentials.Username = null; // this is how we verify the credentials
		}

		private async Task<bool> Authenticate() {
			var data = new FormUrlEncodedContent(new [] {
				new KeyValuePair<string, string>("grant_type", "authorization_code"),
				new KeyValuePair<string, string>("code", _credentials.Password),
				//new KeyValuePair<string, string>("redirect_uri", PinUrl),
				new KeyValuePair<string, string>("client_id", ClientId),
				new KeyValuePair<string, string>("client_secret", ClientSecret),
			});
			var response = await _client.PostAsync(OAuth + "token", data);
			Debug.WriteIf(!response.IsSuccessStatusCode, response.Content.ReadAsStringAsync().Result + $" ({response.StatusCode})", "AniList Token WARNING");
			var json = (JsonObject)JsonValue.Parse(await response.Content.ReadAsStringAsync());
			if(response.StatusCode != HttpStatusCode.OK) throw new ApiRequestException(json?["message"].ToString() ?? response.StatusCode.ToString());
			if(json?["access_token"] == null) throw new ApiRequestException("Null response");
			_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(json["token_type"], json["access_token"]);
			//_client.DefaultRequestHeaders.Add("Authorization", "Bearer " + json["access_token"]);
			_expiration = DateTime.Now.AddSeconds(json["expires_in"]);
			_credentials.Password = json["refresh_token"]; // always use credentials.Password. This will be the refresh token or the auth code!!
			return true;
		}

		private async Task FetchUsername() {
			if(_expiration <= DateTime.Now) Authenticate().Wait();
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
		/// <remarks>Authentication will be done implicitly after the authentication token expires.</remarks>
		/// <returns>True on success</returns>
		public override async Task<bool> VerifyCredentials() {
			await FetchUsername();
			return Username != null;
		}
			
		public async Task<bool> AddAnime(int id, ApiEntry.ListStatuses listStatus) {
			if(_expiration <= DateTime.Now) await Authenticate();

			const string q = @"
				{
					mutation ($id: Int, $status: MediaListStatus) {
						SaveMediaListEntry (mediaId: $id, status: $status) {
							mediaId
					}
				}
			";
			
			var req = new JsonObject() {
				["query"] = q,
				["variables"] = new JsonObject() {
					["id"] = id,
					["status"] = FromListStatus(listStatus)
				}
			};

			var response = await _client.PostAsync(UrlBase, new StringContent(req, Encoding.UTF8, ContentType));
			var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
			if(!response.IsSuccessStatusCode) {
				Debug.WriteLine(json?["errors"] ?? response.StatusCode.ToString(), "AniList AddAnime WARNING");
				throw new ApiRequestException(json?["errors"]?["message"] ?? response.StatusCode.ToString());
			}

			return id == json?["data"]?["SaveMediaListEntry"]["mediaId"];
		}
		
		public async Task<bool> AddAnime(int id) {
			if(_expiration <= DateTime.Now) await Authenticate();

			const string q = @"
				{
					mutation ($id: Int, $status: MediaListStatus) {
						SaveMediaListEntry (mediaId: $id, status: $status) {
							mediaId
					}
				}
			";
			
			var req = new JsonObject() {
				["query"] = q,
				["variables"] = new JsonObject() {
					["id"] = id,
					["status"] = FromListStatus(ApiEntry.ListStatuses.Current)
				}
			};

			var response = await _client.PostAsync(UrlBase, new StringContent(req, Encoding.UTF8, ContentType));
			var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
			if(!response.IsSuccessStatusCode) {
				Debug.WriteLine(json?["errors"] ?? response.StatusCode.ToString(), "AniList AddAnime WARNING");
				throw new ApiRequestException(json?["errors"]?["message"] ?? response.StatusCode.ToString());
			}

			return id == json?["data"]?["SaveMediaListEntry"]["mediaId"];
		}

		public Task<bool> RemoveAnime(int id) {
			throw new System.NotImplementedException();
		}

		public Task<List<Anime>> FindAnime(string keywords) {
			throw new System.NotImplementedException();
		}

		public Task<bool> UpdateAnime(Anime anime) {
			throw new System.NotImplementedException();
		}

		public Task<List<Anime>> PullAnimeList() {
			throw new System.NotImplementedException();
		}
		
		public Task<bool> AddManga(int id, ApiEntry.ListStatuses listStatus) {
			throw new System.NotImplementedException();
		}

		public Task<bool> AddManga(int id) {
			throw new System.NotImplementedException();
		}
		
		public Task<bool> RemoveManga(int id) {
			throw new System.NotImplementedException();
		}

		public Task<bool> UpdateManga(Manga manga) {
			throw new System.NotImplementedException();
		}

		public Task<List<Manga>> FindManga(string keywords) {
			throw new System.NotImplementedException();
		}

		public Task<List<Manga>> PullMangaList() {
			throw new System.NotImplementedException();
		}

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
	}
}