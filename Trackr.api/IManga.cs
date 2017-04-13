using System.Collections.Generic;
using System.Threading.Tasks;

namespace Trackr.api {
    /// <summary>
    /// Represents a manga API client.
    /// </summary>
    public interface IManga {

        Task<bool> VerifyCredentials();
        Task<bool> AddManga();
        void RemoveManga();
        Task<List<Manga>> FindManga(string keywords);
    }
}