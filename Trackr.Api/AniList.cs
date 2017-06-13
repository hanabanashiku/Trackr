using System;
using System.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Trackr.Api {
	public class AniList : Api, IAnime, IManga {

		public new string Name { get; } = "AniList";
		public new string Username;

		// TODO: Find a more secure way of packaging this information
		private const string ClientId = "nolewding-p0hl4";
		private const string ClientSecret = "Vthc0hucRJjJPKAg56PIryD9HBh0A";
		private const string BaseUrl = "https://anilist.co/api/";
		private const string ContentType = "Content-Type: application/x-www-form-urlencoded";

		private static readonly HttpClient Client = new HttpClient();
		
		// these are for using AniList to maintain anime/manga lists - login required.
		private int _expiration;
		private readonly string _pin;
		private string _accessToken;
		private string _refreshToken;
		private int _userId;

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

		public async Task<bool> VerifyCredentials(){
			await AuthenticationCheck();
			var response = await Client.GetAsync("user");
			
			if(!response.IsSuccessStatusCode)
				return false;
			
			var json = JsonValue.Parse(await response.Content.ReadAsStringAsync());
			_userId = json["id"];
			Username = json["display_name"];
			return true;
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
			var json = JsonValue.Parse(response.Content.ReadAsStringAsync().Result);
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
				await Authenticate();
			// Our access token expired, refresh.
			else if((long) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds) > _expiration)
				await Refresh();
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
	}
}

