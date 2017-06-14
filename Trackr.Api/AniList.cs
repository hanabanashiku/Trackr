using System;
using System.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;

namespace Trackr.Api {
	public class AniList : Api, IAnime, IManga {

		public new string Name { get; } = "AniList";
		public new string Username;

		// TODO: Find a more secure way of packaging this information
		private const string ClientId = "nolewding-p0hl4";
		private const string ClientSecret = "Vthc0hucRJjJPKAg56PIryD9HBh0A";
		private const string BaseUrl = "https://anilist.co/api/";
		private const string DateFormat = "yyyyMMdd";
		private const string DateTimeFormat = "yyyy-MM-dd'T'hh:mm:ssZ";
		
		private static readonly HttpClient Client = new HttpClient();
		
		// these are for using AniList to maintain anime/manga lists - login required.
		private int _expiration;
		private readonly string _pin;
		private string _accessToken;
		private string _refreshToken;

		// these are for getting general information in a static context - no login required.
		private static int _clientExpiration;
		private static string _clientAccessToken;

		/// <summary>
		/// Instantiate the AniList client
		/// </summary>
		/// <param name="pin">The OAuth authorization pin given to the user.</param>
		/// <param name="isRefresh">Is this an authorization PIN or a refresh token?</param>
		public AniList(string pin, bool isRefresh = false){
			if(isRefresh)
				_refreshToken = pin;
			else _pin = pin;

			if(!VerifyCredentials().Result)
				throw new ApiRequestException("Could not verify credentials");
			Client.BaseAddress = new Uri(BaseUrl);
		}
		
		/// <summary>
		/// Get the authorization PIN for retrieving an access token.
		/// </summary>
		public async void GetPin(){
			throw new NotImplementedException();
		}

		/// <summary>
		/// Verify the user's credentials and pulls the username of the user.
		/// </summary>
		/// <returns>true on success.</returns>
		public async Task<bool> VerifyCredentials(){
			await AuthenticationCheck();
			var msg = new HttpRequestMessage(HttpMethod.Get, "user");
			msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
			var response = await Client.SendAsync(msg);
			
			if(!response.IsSuccessStatusCode)
				return false;
			
			var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
			Username = json["display_name"];
			return true;
		}

		/// <summary>
		/// Add an anime to the authenticated user's list.
		/// </summary>
		/// <param name="id">The AniList ID of the anime to add.</param>
		/// <param name="status">The list status of the anime.</param>
		/// <returns>true on success.</returns>
		public async Task<bool> AddAnime(int id, ApiEntry.ListStatuses status){
			await AuthenticationCheck();
			
			var content = new FormUrlEncodedContent(new [] {
				new KeyValuePair<string, string>("id", id.ToString()), 	
				new KeyValuePair<string, string>("list_status", FromListStatus(status, true)), 
			});
			var msg = new HttpRequestMessage(HttpMethod.Post, "animelist") {
				Content = content,
				Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _accessToken)}
			};
			var response = await Client.SendAsync(msg);
			return response.IsSuccessStatusCode;
		}

		/// <summary>
		/// Remove an anime from the authenticated user's list
		/// </summary>
		/// <param name="id">The ID of the anime to remove</param>
		/// <returns>true on success</returns>
		public async Task<bool> RemoveAnime(int id){
			await AuthenticationCheck();
			
			var msg = new HttpRequestMessage(HttpMethod.Delete, $"animelist/{id}");
			msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
			var response = await Client.SendAsync(msg);
			return response.IsSuccessStatusCode;
		}

		/// <summary>
		/// Update an anime on the authenticated user's list.
		/// </summary>
		/// <param name="a">The anime to update</param>
		/// <returns>true on success.</returns>
		public async Task<bool> UpdateAnime(Anime a){
			await AuthenticationCheck();
			
			var content = new FormUrlEncodedContent(new [] {
				new KeyValuePair<string, string>("id", a.Id.ToString()), 
				new KeyValuePair<string, string>("list_status", FromListStatus(a.ListStatus, true)), 
				new KeyValuePair<string, string>("score_raw", (a.UserScore * 10).ToString()),
				new KeyValuePair<string, string>("episodes_watched", a.CurrentEpisode.ToString())
			});
			
			var msg = new HttpRequestMessage(HttpMethod.Put, "animelist") {
				Content = content,
				Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _accessToken)}
			};
			var response = await Client.SendAsync(msg);
			return response.IsSuccessStatusCode;
		}

		/// <summary>
		/// Search for an anime from the AniList database.
		/// </summary>
		/// <param name="keywords">The search terms to use.</param>
		/// <returns>The list of results</returns>
		/// <exception cref="ApiRequestException">If the request returns an error code.</exception>
		public async Task<List<Anime>> FindAnime(string keywords){
			await AuthenticationCheck();

			var msg = new HttpRequestMessage(HttpMethod.Get, "anime/search/" + System.Uri.EscapeDataString(keywords));
			msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
			var response = await Client.SendAsync(msg);
			
			if(!response.IsSuccessStatusCode)
				throw new ApiRequestException("Error searching anime: " + response.StatusCode);

			var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
			return (from JsonValue v in json select ToAnime(v)).ToList();
		}

		/// <summary>
		/// Pull a copy of the authenticated user's list from AniList.
		/// </summary>
		/// <returns>A list of all anime list entries.</returns>
		/// <exception cref="ApiRequestException">On bad status code</exception>
		public async Task<List<Anime>> PullAnimeList(){
			await AuthenticationCheck();
			
			var msg = new HttpRequestMessage(HttpMethod.Get, $"user/{Username}/animelist");
			msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
			var response = await Client.SendAsync(msg);
			if(!response.IsSuccessStatusCode)
				throw new ApiRequestException("Error status code: " + response.StatusCode);

			var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
			var ret = new List<Anime>();
			foreach(JsonValue list in json["lists"]) {
				foreach(JsonValue entry in list) {
					var a = ToAnime(entry["anime"]);
					a.ListStatus = ToListStatus(entry["list_status"]);
					a.CurrentEpisode = entry["episodes_watched"];
					a.UserScore = (int) Math.Ceiling(entry["score_raw"] / 10.0);
					a.UserStart = (entry["started_on"] == null)
						? DateTime.MinValue
						: DateTime.ParseExact(entry["started_on"], DateTimeFormat, CultureInfo.InvariantCulture);
					a.UserEnd = (entry["finished_on"] == null)
						? DateTime.MinValue
						: DateTime.ParseExact(entry["finished_on"], DateTimeFormat, CultureInfo.InvariantCulture);
					ret.Add(a);
				}
			}
			return ret;
		}

		// Using the given pin, get the access token
		private async Task<bool> Authenticate(){
			// Use the refresh token if we have it
			if(!string.IsNullOrEmpty(_refreshToken))
				return await Refresh();
			
			var content = new FormUrlEncodedContent(new [] {
				new KeyValuePair<string, string>("grant_type", "authorization_pin"),
				new KeyValuePair<string, string>("client_id", ClientId),
				new KeyValuePair<string, string>("client_secret", ClientSecret),
				new KeyValuePair<string, string>("code", _pin) 
				});
			var response = await Client.PostAsync("auth/access_token", content);
			if(!response.IsSuccessStatusCode)
				return false;
			var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
			_accessToken = json["access_token"];
			_expiration = json["expires"];
			_refreshToken = json["refresh_token"];
			return true;
		}

		// Get a new access token from the server using the refresh token.
		private async Task<bool> Refresh(){
			var content = new FormUrlEncodedContent(new [] {
				new KeyValuePair<string, string>("grant_type", "refresh_token"),
				new KeyValuePair<string, string>("client_id", ClientId),
				new KeyValuePair<string, string>("client_secret", ClientSecret),
				new KeyValuePair<string, string>("refresh_token", _refreshToken)
			});
			var response = await Client.PostAsync("auth/access_token", content);
			if(!response.IsSuccessStatusCode)
				return false;
			var json = JsonValue.Parse(response.Content.ReadAsStringAsync().Result);
			_accessToken = json["access_token"];
			_expiration = json["expires"];
			return true;
		}
		
		// Run this when we need to be sure that the user is authenticated.
		private async Task<bool> AuthenticationCheck(){
			// If we were given a pin, this will get the first auth token
			// If we received a refresh token, this will give us the next one.
			if(string.IsNullOrEmpty(_accessToken))
				return await Authenticate();
			// Our access token expired, refresh.
			if((long) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds) > _expiration)
				return await Refresh();
			return true;
		}

		// this is for gaining a static-context authentication token.
		// this is used for getting database information when not signed 
		// into an AniList account. (e.g. filler for MAL, Kitsu, etc)
		// there is no refresh process, simply repeat this function every hour.
		private static async Task<bool> AuthenticateClient(){
			var content = new FormUrlEncodedContent(new [] {
				new KeyValuePair<string, string>("grant_type", "client_credentials"),
				new KeyValuePair<string, string>("client_id", ClientId),
				new KeyValuePair<string, string>("client_secret", ClientSecret)
			});
			var response = await Client.PostAsync("auth/access_token", content);
			if(!response.IsSuccessStatusCode)
				return false;
			var json = JsonValue.Parse(response.Content.ReadAsStringAsync().Result);
			_clientAccessToken = json["access_token"];
			_clientExpiration = json["expires"];
			return true;
		}
		
		// Run this to be sure the client is authenticated from a static context.
		// If the client's token is expired, it will re-authenticate.
		private static async Task<bool> ClientAuthenticationCheck(){
			// We need a token
			if(string.IsNullOrEmpty(_clientAccessToken))
				return await AuthenticateClient();
			// our token has expired, get a new one
			if((long) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds) > _clientExpiration)
				return await AuthenticateClient();
			// We are golden
			return true;
		}

		// Convert from list status to string.
		// params: the status, is it an anime or manga?
		private static string FromListStatus(ApiEntry.ListStatuses status, bool anime){
			switch(status) {
				case ApiEntry.ListStatuses.Completed:
					return "completed";
				case ApiEntry.ListStatuses.Current:
					return anime ? "watching" : "reading";
				case ApiEntry.ListStatuses.Dropped:
					return "dropped";
				case ApiEntry.ListStatuses.OnHold:
					return "on-hold";
				case ApiEntry.ListStatuses.Planned:
					return anime ? "plan to watch" : "plan to read";
				default:
					return string.Empty;
			}
		}

		// convert a string received from AniList to a ListStatus
		private static ApiEntry.ListStatuses ToListStatus(string status){
			switch(status) {
				case "completed":
					return ApiEntry.ListStatuses.Completed;
				case "watching": case "reading":
					return ApiEntry.ListStatuses.Current;
				case "dropped":
					return ApiEntry.ListStatuses.Dropped;
				case "on-hold":
					return ApiEntry.ListStatuses.OnHold;
				case "plan to watch": case "plan to read":
					return ApiEntry.ListStatuses.Planned;
				default:
					return ApiEntry.ListStatuses.NotInList;
			}
		}

		// convet a string received from AniList to a ShowType
		private static Anime.ShowTypes ToShowType(string type){
			switch(type) {
				case "TV": case "TV short":
					return Anime.ShowTypes.Tv;
				case "Movie":
					return Anime.ShowTypes.Movie;
				case "Special":
					return Anime.ShowTypes.Special;
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

		// find running status of a series based on given start and end time
		private static Anime.RunningStatuses ResolveRunningStatus(DateTime start, DateTime end){
			if(start == DateTime.MinValue || start > DateTime.Now)
				return Anime.RunningStatuses.NotYetAired;
			if(end == DateTime.MinValue || end < DateTime.Now)
				return Anime.RunningStatuses.Airing;
			return Anime.RunningStatuses.Completed;
		}

		// Convert a set of JsonValues received from AniList to an Anime object.
		private static Anime ToAnime(JsonValue a){
			string[] syn = new string[a["synonyms"].Count];
			int i = 0;
			foreach(JsonValue s in a["synonyms"]) {
				syn[i] = s;
				i++;
			}

			DateTime start = (a["start"] == null)
				? DateTime.MinValue
				: DateTime.ParseExact(a["start"], DateFormat, CultureInfo.InvariantCulture);
			DateTime end = (a["end"] == null)
				? DateTime.MinValue
				: DateTime.ParseExact(a["end"], DateFormat, CultureInfo.InvariantCulture);
			
			return new Anime(a["id"], a["title_romaji"], a["title_english"],
				a["title_japanese"], syn, a["total_episodes"], a["average_score"]/10, 
				ToShowType(a["type"]), ResolveRunningStatus(start, end), start, end, 
					a["description"], a["image_url_lge"]);
		}
	}
}

