using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Json;
using System.Net;
using System.Net.Http.Headers;
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
        private const string Users = UrlBase + "/users";

        private readonly HttpClient _client;
        private UserPass _clientLogin;
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
        public async Task<bool> VerifyCredentials(){
            if(await Authenticate() == false)
                return false;

            // TODO: Get _userId
            var json = new JsonObject() {

            };

            return true;
        }

        public async Task<bool> AddAnime(int id, ApiEntry.ListStatuses status){
            if(DateTime.Now >= _expiration) await Authenticate();

            var data = new JsonObject() {
                ["data"] = new JsonObject {
                    ["type"] = "libraryEntries",
                    ["attributes"] = new JsonObject() {
                        ["status"] = ListStatusToString(status)
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

            var response = await _client.PostAsync(UrlBase, new StringContent(data));
            return response.StatusCode == HttpStatusCode.OK;
        }

        private static string ListStatusToString(ApiEntry.ListStatuses status){
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
                    return string.Empty;
            }
        }

        private static ApiEntry.ListStatuses StringToListStatus(string status){
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
                default: // should not be received from the server
                    return ApiEntry.ListStatuses.NotInList;
            }
        }
    }
}