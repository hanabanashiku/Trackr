using System;
using System.Collections.Generic;
using System.Json;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Trackr.Core;

namespace Trackr.Api {
	public class AniList : Api, IAnime, IManga {
		public const string Identifier = "AniList";
		/// <summary>
		/// The name of the current API
		/// </summary>
		public override string Name => Identifier;

		/// <summary>
		/// The username of the account signed in to AniList.
		/// </summary>
		public override string Username => _credentials.Username;
		
		private const string UrlBase = "https://graphql.anilist.co";
		private const string OAuth = "https://anilist.co/api/v2/oauth/";
		private const string ClientId = "289";
        private const string ClientSecret = "SS3LOMIbG2hvfIgPoWcxVXdFGGDLI687owlfGbSa";
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

		private async Task FetchAccessToken() {
			var json = new JsonObject() {
				["grant_type"] = "authorization_code",
				["client_id"] = ClientId,
				["client_secret"] = ClientSecret,
				["code"] = _credentials.Password
			};
			var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, OAuth + "token") {
				Content = new StringContent(json.ToString(), Encoding.UTF8, ContentType)
			});
			if(response.StatusCode != HttpStatusCode.OK) throw new ApiRequestException(response.StatusCode.ToString());
			json = (JsonObject)JsonValue.Parse(await response.Content.ReadAsStringAsync());
			if(json?["access_token"] == null) throw new ApiRequestException("Null response");
			_client.DefaultRequestHeaders.Add("Bearer", json["access_token"]);
			_expiration = DateTime.Now.AddSeconds(json["expires_in"]);
		}

		private async Task FetchUsername() {
			if(_expiration > DateTime.Now) await FetchAccessToken();
			const string q = "query() { Viewer { id, name } }";
			var response = await _client.PostAsync(UrlBase, new StringContent(q));
			if(!response.IsSuccessStatusCode) throw new ApiRequestException(response.StatusCode.ToString());
			var json = (JsonObject)JsonValue.Parse(await response.Content.ReadAsStringAsync());
			if(json == null) throw new ApiFormatException("Null response");
			_credentials.Username = json["data"]["name"];
			_userId = json["data"]["id"];
		}

		/// <summary>
		/// Running this function authenticates the user with AniList's OAuth2 service.
		/// </summary>
		/// <remarks>Authentication will be done implicitly after the authentication token expires.</remarks>
		/// <returns>True on success</returns>
		public override async Task<bool> VerifyCredentials() {
			if(_expiration > DateTime.Now) await FetchAccessToken();
			await FetchUsername();
			return Username != null;
		}

		public Task<bool> AddManga(int id, ApiEntry.ListStatuses listStatus) {
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

		public Task<bool> AddAnime(int id, ApiEntry.ListStatuses listStatus) {
			throw new System.NotImplementedException();
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
	}
}