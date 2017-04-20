using System.Collections.Generic;
using System.Threading.Tasks;

namespace Trackr.Api {
    /// <summary>
    /// Represents an anime API client
    /// </summary>
    public interface IAnime {
        Task<bool> VerifyCredentials();
        Task<bool> AddAnime(int id, ApiEntry.ListStatuses listStatus);
        Task<bool> RemoveAnime(int id);
        Task<bool> UpdateAnime(Anime anime);
        Task<List<Anime>> FindAnime(string keywords);
        Task<List<Anime>> PullAnimeList();
    }
}