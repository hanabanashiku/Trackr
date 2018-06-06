using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Trackr.Core;
using Trackr.Api;

namespace Trackr.List {
    /// <summary>
    /// A class for managing anime API calls and storing them
    /// </summary>
    [Serializable]
    public class AnimeList : IEnumerable<Anime> {
        [NonSerialized]
        private IAnime _client;
        /// <summary>
        /// The client this list is using
        /// </summary>
        //public IAnime Client => _client;

        private readonly List<Anime> _entries;
        private readonly List<Anime> _queue; // these must be synced
		public string Api => _client.Name;
        public string Username => _client.Username; // the username of the api instance

        private readonly string _filePath;

        /// <summary>
        /// Instantiate the anime list
        /// </summary>
        private AnimeList(IAnime client){
            _entries = new List<Anime>();
            _queue = new List<Anime>();
            _client = client;
            _filePath = ResolveFilePath(client);
        }

        /// <summary>
        /// Load the list and initialize it.
        /// </summary>
        /// <param name="client">The API client to use</param>
        /// <returns></returns>
        public static AnimeList Load(IAnime client) {
            AnimeList list = null;
            try {
                var f = new BinaryFormatter();
                var fs = new FileStream(ResolveFilePath(client), FileMode.Open, FileAccess.Read, FileShare.Read);
                var t = (AnimeList)f.Deserialize(fs);
                fs.Close();
                t._client = client;
                list = t;
                Task.Run(() => t.Sync());
                return list;
            }
            catch(FileNotFoundException) {
                list = new AnimeList(client);
                list.Sync().Wait();
                return list;
            }
            catch(ApiRequestException) {
                Debug.WriteLine("Anime List sync failed.");
                if(list == null) throw;
                return list;
            }
        }

        /// <summary>
        /// Add an anime to the list and queue it for syncing.
        /// </summary>
        /// <param name="a">The anime to add.</param>
        public void Add(Anime a){
            if(a.ListStatus == ApiEntry.ListStatuses.NotInList)
                a.ListStatus = ApiEntry.ListStatuses.Current; // Default value
            // ignore if it's already there.
            // through the magic of OOP, references get updated everywhere.
            // the idea is that if the anime is in the list, we will be given that reference
            if(Contains(a)) return;
            if(!_queue.Contains(a))
                _queue.Add(a);
            _entries.Add(a);
        }

        public void Remove(Anime a){
            a.ListStatus = ApiEntry.ListStatuses.NotInList;

            if(!_queue.Contains(a))
                _queue.Add(a);
            _entries.Remove(this[a.Id]);
        }

        /// <summary>
        /// Send the changes to an anime to the server
        /// </summary>
        /// <param name="a">An anime reference from this list.</param>
        public void Update(Anime a){
            if(!Contains(a))
                Add(a);
            if(!_queue.Contains(a))
                _queue.Add(a);
        }

        /// <summary>
        /// Returns anime query results with list information included.
        /// </summary>
        /// <param name="keywords">The search terms to use</param>
        /// <returns>A list of all anime results.</returns>
        /// <exception cref="ApiRequestException">if the request times out.</exception>
        /// <remarks>Some APIs have return count limits set</remarks>
        public async Task<List<Anime>> Find(string keywords){
            var result = await _client.FindAnime(keywords);

            // update references if we already have the data in our list
            for(var i = 0; i < result.Count; i++)
                if(Contains(result[i]))
                    result[i] = this[result[i].Id];
            return result;
        }

        /// <summary>
        /// Sync tasks that are currently in the queue.
        /// </summary>
        /// <exception cref="ApiFormatException">if the request times out.</exception>
        /// <remarks>The SyncStart, SyncStop, and SyncError events send error message or success indicators!</remarks>
        public async Task<bool> Sync() {
            var remote = await _client.PullAnimeList();
            /*   CASES
                1. The anime is in remote, but not in list
                    a. The anime is enqueued for deletion - delete from server
                    b. The anime is not enqueued for deletion - it was added to the server; add it to the list
                2. The anime is in the list, but not in remote
                    a. The anime is enqueued for addition - add it to the server
                    b. The anime is not enqueued for addition - it was deleted; remove it from the list
                3. The anime is in the list, and in remote.
                    a. The anime is enqueued for updating - update it on the server and keep our copy.
                    b. The anime is not enqueued for updating - keep the server's copy; the values could have changed.
             */

            // First lets partition our lists.
            var remoteUnique = remote.Except(_entries).ToList(); // remote - entries
            var clientUnique = _entries.Except(remote).ToList(); // entries - remote
            var common = _entries.Except(remoteUnique).Except(clientUnique).ToList(); // entries u remote

            // Case 1: unique to server
            foreach(var a in remoteUnique) {
                if(!_queue.Contains(a))
                    _entries.Add(a);
                else if(_queue.First(x => x.Id == a.Id).ListStatus == ApiEntry.ListStatuses.NotInList) {
                    await _client.RemoveAnime(a.Id);
                    _queue.Remove(a);
                }
            }

            // Case 2: unique to client
            foreach(var a in clientUnique) {
                if(!_queue.Contains(a))
                    _entries.Remove(a);
                else if(_queue.First(x => x.Id == a.Id).ListStatus != ApiEntry.ListStatuses.NotInList) {
                    await _client.AddAnime(a.Id, a.ListStatus);
                    await _client.UpdateAnime(a);
                    _queue.Remove(a);
                }
                else {
                    _entries.Remove(a);
                    _queue.Remove(a);
                }
            }

            // Case 3: common to both
            foreach(var a in common) {
                if(!_queue.Contains(a)) {
                    a.Replace(remote.First(x => x.Id == a.Id)); // take the server values
                }
                else if(_queue.First(x => x.Id == a.Id).ListStatus != ApiEntry.ListStatuses.NotInList) {
                    await _client.UpdateAnime(a);
                    _queue.Remove(a);
                }
            }

            if(_queue.Count != 0) {
                // Something may have gone wrong here
                Debug.WriteLine("Queue is not empty!");
                _queue.ForEach(x => Debug.WriteLine($"{x.Title}, {x.ListStatus}, {x.CurrentEpisode}"));
            }
            return true;
        }

        /// <summary>
        /// Save changes to the list to the disk.
        /// </summary>
        public void Save(){
            var f = new BinaryFormatter();
            if (!Directory.Exists(Program.AppDataPath))
                Directory.CreateDirectory(Program.AppDataPath);
            var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            f.Serialize(fs, this);
            fs.Close();
        }

        /// <summary>
        /// Get anime by ID
        /// </summary>
        /// <returns>The requested anime, or null if it doesn't exist.</returns>
        public Anime this[int id]{
            get {
                try {
                    return _entries.First(x => x.Id == id);
                }
                catch(Exception) {
                    return null;
                }
            }
        }

        /// <summary>
        /// Get specific list (watching, planned, etc.)
        /// </summary>
        public List<Anime> this[ApiEntry.ListStatuses i] {
            get { return _entries.Where(x => x.ListStatus == i).ToList(); }
        }

        /// <param name="a">The anime to check for</param>
        /// <returns>True if the list contains an anime with a matching ID.</returns>
        public bool Contains(Anime a) {
            return Contains(a.Id);
        }

        /// <param name="id">The anime ID to check for</param>
        /// <returns>True if the list contains an anime with a matching ID.</returns>
        public bool Contains(int id){
            return _entries.Exists(x => x.Id == id);
        }

        public int Count(){
            return _entries.Count;
        }

        public IEnumerator<Anime> GetEnumerator(){
            return _entries.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator(){
            return GetEnumerator();
        }

        ~AnimeList(){
            Save();
        }

        private static string ResolveFilePath(IAnime cli){
            return Path.Combine(Program.AppDataPath, $"animelist.{cli.Name}.{cli.Username}db");
        }
    }
}