using System;
using System.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

using Trackr.Core;

namespace Trackr.Api {
	public class AniList : Api, IAnime, IManga {

		public new string Name { get; } = "AniList";
		public new string Username => _clientLogin.Username;

		private const string ClientId = "nolewding-p0hl4";
		private const string ClientSecret = "Vthc0hucRJjJPKAg56PIryD9HBh0A";
		private const string BaseUrl = "https://anilist.co/api/";
		private const string AuthUrl = BaseUrl + "auth/";

		private static readonly HttpClient _client = new HttpClient();
		private readonly UserPass _clientLogin;
		// these are for using AniList to maintain anime/manga lists - login required.
		private DateTime _expiration;
		private string _accessToken;
		private string _refreshToken;
		// these are for getting general information in a static context - no login required.
		private static DateTime _clientExpiration;
		private static string _clientAccessToken;

		public AniList(UserPass credentials) {
			_clientLogin = credentials;
			Authenticate();
		}

		// 
		private async Task<bool> Authenticate(){
			//TODO: finish this when we GUI is complete, so we can retrieve a authentication PIN via web window.
			throw new NotImplementedException();
		}

		// Get a new access token from the server using the refresh token.
		private async Task<bool> Refresh(){
			var content = new FormUrlEncodedContent(new [] {
				new KeyValuePair<string, string>("grant_type", "refresh_token"),
				new KeyValuePair<string, string>("client_id", ClientId),
				new KeyValuePair<string, string>("client_secret", ClientSecret),
				new KeyValuePair<string, string>("refresh_token", _refreshToken)
			});
			var response = await _client.PostAsync($"{AuthUrl}access_token", content);
			if(!response.IsSuccessStatusCode)
				return false;
			var json = JsonValue.Parse(response.Content.ReadAsStringAsync().Result);
			_accessToken = json["access_token"];
			_expiration = DateTime.Now.AddSeconds(json["expires_in"]);
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
			var response = await _client.PostAsync($"{AuthUrl}access_token", content);
			if(!response.IsSuccessStatusCode)
				return false;
			var json = JsonValue.Parse(response.Content.ReadAsStringAsync().Result);
			_clientAccessToken = json["access_token"];
			_clientExpiration = DateTime.Now.AddSeconds(json["expires_in"]);
			return true;
		}
	}
}

