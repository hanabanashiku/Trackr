using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Trackr.Api {
    /// <summary>
    /// Represents an anime API client
    /// </summary>
    public interface IAnime {
        /// <summary>
        /// The name of the current API.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// The username of the user logged into the API.
        /// </summary>
        string Username { get; }

        /// <summary>
        /// Verify provided credentials with the current API.
        /// </summary>
        Task<bool> VerifyCredentials();
        /// <summary>
        /// Add an anime to the authenticated user's list.
        /// </summary>
        /// <param name="id">The ID number of the given anime</param>
        /// <param name="listStatus">The listStatus to add it under (default is Currently Watching)</param>
        /// <returns>true on success.</returns>
        /// <exception cref="ApiRequestException">If the anime already exists in the user's list.</exception>
        /// <exception cref="ArgumentException">If the anime list status is set to be "not in list".</exception>
        Task<bool> AddAnime(int id, ApiEntry.ListStatuses listStatus);
        /// <summary>
        /// Remove an anime from the authenticated user's list.
        /// </summary>
        /// <param name="id">The ID of the anime to remove</param>
        /// <returns>true on success.</returns>
        Task<bool> RemoveAnime(int id);
        /// <summary>
        /// Search for an anime in the API's database.
        /// </summary>
        /// <param name="keywords">The search term to use</param>
        /// <returns>A list of all anime found.</returns>
        /// <exception cref="ApiFormatException">if the returned node(s) are malformed.</exception>
        Task<List<Anime>> FindAnime(string keywords);
        /// <summary>
        /// Update the given entry on the authenticated user's list.
        /// </summary>
        /// <param name="anime">The anime to update, with updated values.</param>
        /// <returns>true on success</returns>
        Task<bool> UpdateAnime(Anime anime);
        /// <summary>
        /// Get the authenticated user's anime list from the server.
        /// </summary>
        /// <returns>A list of all anime in the user's list</returns>
        /// <exception cref="ApiFormatException">if the response is malformed.</exception>
        Task<List<Anime>> PullAnimeList();
    }
}