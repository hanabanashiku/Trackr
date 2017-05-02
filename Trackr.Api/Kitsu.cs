using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Json;
using System.Net;
using Trackr.Core;

namespace Trackr.Api {
    /// <summary>
    /// A class for handling instances of Kitsu (formerly Hummingbird) accounts.
    /// </summary>
    public class Kitsu : Api, IAnime, IManga {
        public new string Name { get; } = "Kitsu";
        public new string Username => _clientLogin.Username;

        private const string ContentType = "application/vnd.api+json";
        private const string AuthUrl = "https://kitsu.io/api/oauth/token";
        // These are defaults, as app registration is not yet possible on Kitsu
        private const string ClientId = "dd031b32d2f56c990b1425efe6c42ad847e7fe3ab46bf1299f05ecd856bdb7dd";
        private const string ClientSecret = "54d7307928f63414defd96399fc31ba847961ceaecef3a5fd93144e960c0e151";
        private const string UrlBase = "https://kitsu.io/api/17";
        private const string Entries = "library-entries";
        private const string Anime = "anime";
        private const string Manga = "manga";

        private readonly HttpClient _client;
        private UserPass _clientLogin;
        private string _accessToken;
        private DateTime _expiration;

        public Kitsu(UserPass credentials){
            _clientLogin = credentials;
            _client = new HttpClient();
            _client.DefaultRequestHeaders
                .TryAddWithoutValidation("accept", ContentType);
            _accessToken = null;
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
            var json = JsonValue.Parse(response.Content.ReadAsStringAsync().Result);
            _accessToken = json["access_token"];
            _expiration = DateTime.Now.AddSeconds(double.Parse(json["expires_in"]));
            return true;
        }

        /// <summary>
        /// Running this function from the Kitsu API authenticates with the Kitsu OAuth2 service.
        /// </summary>
        /// <returns>True on success (200OK), false on failure (400, 401)</returns>
        /// <remarks>Authentication will be done implicitly  after the token expires.</remarks>
        public async Task<bool> VerifyCredentials(){
            return await Authenticate();
        }
    }
}