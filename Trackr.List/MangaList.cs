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
    /// A class for managing manga API calls and storing them
    /// </summary>
    [Serializable]
    public class MangaList : IEnumerable<Manga> {
        [NonSerialized]
        private IManga _client;
        /// <summary>
        /// The client this list is using
        /// </summary>
        public IManga Client => _client;

        private List<Manga> _entries;
        private readonly Queue<Manga> _queue; // these must be synced
        public string Api => Client.Name;
        public string Username => Client.Username; // the username of the api instance

        private readonly string _filePath;

        /// <summary>
        /// Instantiate the anime list
        /// </summary>
        private MangaList(){
            _entries = new List<Manga>();
            _queue = new Queue<Manga>();
            _filePath = ResolveFilePath(_client);
        }

        /// <summary>
        /// Load the list and initialize it.
        /// </summary>
        /// <param name="client">The API client to use</param>
        /// <returns></returns>
        public static MangaList Load(IManga client){
            try {
                var f = new BinaryFormatter();
                var fs = new FileStream(ResolveFilePath(client), FileMode.Open, FileAccess.Read, FileShare.Read);
                MangaList list = (MangaList) f.Deserialize(fs);
                fs.Close();
                return list;
            }
            catch(Exception) {
                MangaList list = new MangaList {_client = client};
                list.Sync();
                return list;
            }
        }

        /// <summary>
        /// Add a manga to the list and queue it for syncing.
        /// </summary>
        /// <param name="m">The manga to add.</param>
        /// <remarks>This requires a reference to an object because MAL does not support fetching by ID number in any
        /// concise way. Use Find() to retrieve.</remarks>
        public void Add(Manga m){
            if(m.ListStatus == ApiEntry.ListStatuses.NotInList)
                m.ListStatus = ApiEntry.ListStatuses.Current; // Default value
            // ignore if it's already there.
            // through the magic of OOP, references get updated everywhere.
            if(Contains(m)) return;
            if(!_queue.Contains(m))
                _queue.Enqueue(m);
            _entries.Add(m);
        }

        public void Remove(Manga m){
            m.ListStatus = ApiEntry.ListStatuses.NotInList;

            if(!_queue.Contains(m))
                _queue.Enqueue(m);
            _entries.RemoveAll(x => x.Id == m.Id);
        }

        public void Update(Manga m){
            if(!Contains(m))
                Add(m);
            else if(!_queue.Contains(m))
                _queue.Enqueue(m);
        }

        /// <summary>
        /// Returns manga query results with list information included.
        /// </summary>
        /// <param name="keywords">The search terms to use</param>
        /// <returns>A list of all anime results.</returns>
        /// <exception cref="ApiFormatException">if the request times out.</exception>
        public async Task<List<Manga>> Find(string keywords){
            var result = await _client.FindManga(keywords);
            return result.Count == 0 ? result
                : result.Select(m => Contains(m) ? this[m.Id] : m).ToList();
        }

        /// <summary>
        /// Sync tasks that are currently in the queue.
        /// </summary>
        /// <exception cref="ApiFormatException">if the request times out.</exception>
        public async Task<bool> Sync() {
            var remote = await _client.PullMangaList();
            // First we update from our sync queue.
            while(_queue.Count != 0) {
                var m = _queue.Peek();
                // We want to add it and it's not already there
                if(!remote.Contains(m) && m.ListStatus != ApiEntry.ListStatuses.NotInList)
                    await _client.AddManga(m.Id, m.ListStatus);
                await _client.UpdateManga(m); // calls RemoveAnime() implicitly if NotInList
                _queue.Dequeue(); // NOTE: even if it is rejected, it is still being dequeued.
            }
            // Pull again after we have exhausted our sync queue
            _entries = await _client.PullMangaList();
            return true;
        }

        /// <summary>
        /// Save changes to the list to the disk.
        /// </summary>
        public void Save(){
            var f = new BinaryFormatter();
            var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            f.Serialize(fs, this);
            fs.Close();
        }

        /// <summary>
        /// Get anime by ID
        /// </summary>
        public Manga this[int i]{
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
        public List<Manga> this[ApiEntry.ListStatuses i] {
            get { return _entries.Where(x => x.ListStatus == i).ToList(); }
        }

        public bool Contains(Manga m){
            return _entries.Contains(m);
        }
        public bool Contains(int id){
            return _entries.Exists(x => x.Id == id);
        }

        public IEnumerator<Manga> GetEnumerator(){
            return _entries.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator(){
            return GetEnumerator();
        }

        ~MangaList(){
            Save();
        }

        private static string ResolveFilePath(IManga cli){
            return Path.Combine(Program.AppDataPath, $"mangalist.{cli.Name}.{cli.Username}db");
        }
    }
}