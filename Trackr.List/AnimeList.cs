using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
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
        private readonly Queue<Anime> _queue; // these must be synced
		public string Api => _client.Name;
        public string Username => _client.Username; // the username of the api instance

        private readonly string _filePath;

        public event EventHandler SyncStart;
        public event EventHandler SyncStop;
        public event ErrorEventHandler SyncError;

        /// <summary>
        /// Instantiate the anime list
        /// </summary>
        private AnimeList(IAnime client){
            _entries = new List<Anime>();
            _queue = new Queue<Anime>();
            _client = client;
            _filePath = ResolveFilePath(client);
        }

        /// <summary>
        /// Load the list and initialize it.
        /// </summary>
        /// <param name="client">The API client to use</param>
        /// <returns></returns>
        public static AnimeList Load(IAnime client){
            try {
                var f = new BinaryFormatter();
                var fs = new FileStream(ResolveFilePath(client), FileMode.Open, FileAccess.Read, FileShare.Read);
                var list = (AnimeList) f.Deserialize(fs);
                fs.Close();
                list._client = client;
                Task.Run(() => list.Sync());
                return list;
            }
            catch(FileNotFoundException) {
                var list = new AnimeList(client);
                list.Sync().Wait();
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
                _queue.Enqueue(a);
            _entries.Add(a);
        }

        public void Remove(Anime a){
            a.ListStatus = ApiEntry.ListStatuses.NotInList;

            if(!_queue.Contains(a))
                _queue.Enqueue(a);
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
                _queue.Enqueue(a);
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

            // replace search with list data where possible
            for(var i = 0; i < result.Count; i++) {
                var a = this[result[i].Id]; // from list
                if(a != null) result[i] = a;
            }
            return result;
        }
        
        /// <summary>
        /// Sync tasks that are currently in the queue.
        /// </summary>
        /// <exception cref="ApiFormatException">if the request times out.</exception>
        /// <remarks>The SyncStart, SyncStop, and SyncError events send error message or success indicators!</remarks>
        public async Task<bool> Sync() {
            SyncStart?.Invoke(this, EventArgs.Empty); // We're syncing!
            try {
                var remote = await _client.PullAnimeList();

                // Pull everything we aren't changing from the server
                foreach(var a in remote.Except(_queue)) {
                    if(!Contains(a)) _entries.Add(a); // it's not there, add it
                    // check if the values are same. If not, we want to preserve the references
                    // note: if user values are unchanged, server values will be unchanged too. Usually not an issue..
                    else if(a != this[a.Id])
                        this[a.Id].Replace(a);
                }
                
                // Add everything from the server that's not already on the list.
                _entries.AddRange(remote.Except(_entries));
                
                // For now we will prefer our app's version. 
                // Collisions should be infrequent, and if there are, the values will likely be similar
                // Possibly we could make use of UpdatedAt values from the server (but MAL doesn't have these..)
                while(_queue.Count != 0) { // start server updating
                    var a = _queue.Peek();
                    if(!remote.Contains(a) && a.ListStatus != ApiEntry.ListStatuses.NotInList) {
                        if(!await _client.AddAnime(a.Id, a.ListStatus)) {
                            SyncError?.Invoke(this, new ErrorEventArgs(new Exception($"Adding anime {a.Title} on server failed!")));
                            return false;
                        }
                    }

                    // Always call update, even if we've added something
                    // Note RemoveAnime() is being implicitly called when necessary
                    if(!await _client.UpdateAnime(a)) { 
                        SyncError?.Invoke(this, new ErrorEventArgs(new Exception($"Updating anime {a.Title} on server failed!")));
                        return false;
                    }
                    _queue.Dequeue(); // if successful
                } // end server updating
            }
            catch(Exception e) {
                SyncError?.Invoke(this, new ErrorEventArgs(e.InnerException ?? e)); // Error!
                return false;
            }
            SyncStop?.Invoke(this, EventArgs.Empty); // we're done!
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