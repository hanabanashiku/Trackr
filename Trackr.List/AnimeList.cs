using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Trackr.Core;
using Trackr.Api;

namespace Trackr.List {
    /// <summary>
    /// A class for managing anime API calls and storing them
    /// </summary>
    // TODO: Add, remove, update handles.
    [Serializable]
    public class AnimeList : IEnumerable<Anime> {
        [NonSerialized]
        private IAnime _client;
        /// <summary>
        /// The client this list is using
        /// </summary>
        public IAnime Client => _client;

        private List<Anime> _entries;
        private Queue<Anime> _queue; // these must be synced

        private readonly string _filePath;

        /// <summary>
        /// Instantiate the anime list
        /// </summary>
        private AnimeList(){
            _entries = new List<Anime>();
            _queue = new Queue<Anime>();
            _filePath = ResolveFilePath(_client);
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
                AnimeList list = (AnimeList) f.Deserialize(fs);
                fs.Close();
                return list;
            }
            catch(Exception) {
                AnimeList list = new AnimeList {_client = client};
                list.Sync();
                return list;
            }
        }

        public async void Sync(){
            var remote = await _client.PullAnimeList();
            // First we update from our sync queue.
            while(_queue.Count != 0) {
                var a = _queue.Peek();
                if(!remote.Contains(a) && a.ListStatus != ApiEntry.ListStatuses.NotInList)
                    await _client.AddAnime(a.Id, a.ListStatus);
                await _client.UpdateAnime(a);
                _queue.Dequeue();
            }

            // Pull again after we have exhausted our sync queue
            _entries = await _client.PullAnimeList();
        }

        /// <summary>
        /// Save changes to the list to the disk
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