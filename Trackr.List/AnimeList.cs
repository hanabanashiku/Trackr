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
        private readonly IAnime _client;
        /// <summary>
        /// The client this list is using
        /// </summary>
        public IAnime Client => _client;

        private List<Anime> _entries;
        private readonly Queue<Anime> _queue; // these must be synced
		public string Api => Client.Name;
        public string Username => Client.Username; // the username of the api instance

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
                return list;
            }
            catch(Exception) {
                var list = new AnimeList(client);
                list.Sync().Wait();
                return list;
            }
        }

        /// <summary>
        /// Add an anime to the list and queue it for syncing.
        /// </summary>
        /// <param name="a">The anime to add.</param>
        /// <remarks>This requires a reference to an object because MAL does not support fetching by ID number in any
        /// concise way. Use Find() to retrieve.</remarks>
        public void Add(Anime a){
            if(a.ListStatus == ApiEntry.ListStatuses.NotInList)
                a.ListStatus = ApiEntry.ListStatuses.Current; // Default value
            // ignore if it's already there.
            // through the magic of OOP, references get updated everywhere.
            if(Contains(a)) return;
            if(!_queue.Contains(a))
                _queue.Enqueue(a);
            _entries.Add(a);
        }

        public void Remove(Anime a){
            a.ListStatus = ApiEntry.ListStatuses.NotInList;

            if(!_queue.Contains(a))
                _queue.Enqueue(a);
            _entries.RemoveAll(x => x.Id == a.Id);
        }

        public void Update(Anime a){
            if(!Contains(a))
                Add(a);
            else if(!_queue.Contains(a))
                _queue.Enqueue(a);
        }

        /// <summary>
        /// Returns anime query results with list information included.
        /// </summary>
        /// <param name="keywords">The search terms to use</param>
        /// <returns>A list of all anime results.</returns>
        /// <exception cref="ApiFormatException">if the request times out.</exception>
        public async Task<List<Anime>> Find(string keywords){
            var result = await _client.FindAnime(keywords);
            
            // Get updated list data for everything we have in our list already
            for(var i = 0; i < result.Count; i++) {
                var a = _entries.FirstOrDefault(x => x.Id == result[i].Id);
                if(a != null) result[i] = a;
            }
            
            return result.Select(a => Contains(a) ? this[a.Id] : a).ToList();
        }

        /// <summary>
        /// Sync tasks that are currently in the queue.
        /// </summary>
        /// <exception cref="ApiFormatException">if the request times out.</exception>
        // TODO: Make this more efficient etc
        public async Task<bool> Sync() {
            SyncStart?.Invoke(this, EventArgs.Empty);
            try {
                var remote = await _client.PullAnimeList();
                // First we update from our sync queue.
                while(_queue.Count != 0) {
                    var a = _queue.Dequeue();
                    // We want to add it and it's not already there
                    if(!remote.Contains(a) && a.ListStatus != ApiEntry.ListStatuses.NotInList) {
                        if(await _client.AddAnime(a.Id, a.ListStatus) == false) {
                            SyncStop?.Invoke(this, EventArgs.Empty);
                            return false;
                        }
                            
                    }
                    if(await _client.UpdateAnime(a) == false){ // calls RemoveAnime() implicitly if NotInList        
                        SyncStop?.Invoke(this, EventArgs.Empty);
                        return false;
                    }
                }
                // Pull again after we have exhausted our sync queue
                _entries = await _client.PullAnimeList();
                SyncStop?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch(Exception e) {
                SyncError?.Invoke(this, new ErrorEventArgs(e));
                return false;
            }
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
        public Anime this[int i]{
            get {
                try {
                    return _entries.First(x => x.Id == i);
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

        public bool Contains(Anime a){
            return _entries.Contains(a);
        }
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