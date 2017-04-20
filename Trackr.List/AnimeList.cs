using System.Data.SQLite;
using Trackr.Core;
using Trackr.Api;

namespace Trackr.List {
    /// <summary>
    /// A class for managing anime API calls and storing them
    /// </summary>
    public class AnimeList {
        private SQLiteConnection _db;
        private static readonly string Path = System.IO.Path.Combine(Program.AppDataPath, "anime.db");
        private IAnime _client;

        /// <summary>
        /// Instantiate the anime list
        /// </summary>
        /// <param name="client">The API client to use</param>
        public AnimeList(IAnime client){
            _client = client;
            _db = new SQLiteConnection(Path);
            //TODO: Setup database stuff
        }
    }
}