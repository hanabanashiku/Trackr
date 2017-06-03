using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;

namespace Trackr.Torrent {
    /// <summary>
    /// Represents a torrent pulled from a feed.
    /// </summary>
    public class Torrent {
        /// <summary>
        /// The name of the torrent file
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The torrent's URI (either a magnet link or a .torrent link)
        /// </summary>
        public Uri Url { get; }
        public string SubGroup { get; private set; }
        public string Anime { get; private set; }
        public int Episode { get; private set; }

        internal Torrent(SyndicationItem item){
            Url = item.Links[0].Uri;
            Name = item.Title.Text;
            ParseTitle();
        }

        private void ParseTitle(){
            // [SubGroup] Anime Title - 01 [720p].mp4
            Regex r = new Regex(@"\[([a-zA-Z-_]+)\]\s*(.+)\s*-\s*([0-9]+)\s*[[(](.+)[)\]](\.[a-zA-Z0-9]{3})?");
            Match m = r.Match(Name);
            SubGroup = m.Groups[0].Value;
            Anime = m.Groups[1].Value;
            Episode = int.Parse(m.Groups[2].Value);
            // TODO: Parse the video quality capture group.
        }

        /// <summary>
        /// Download the torrent using the default torrent program.
        /// </summary>
        public async void Download(){
            // it's a magnet link, download it.
            if(Url.Scheme == "magnet") 
                Process.Start(Url.AbsoluteUri);
            // It's a .torrent file, download the file, and then run it.
            else if(Path.GetExtension(Url.AbsolutePath) == "torrent") {
                // TODO: Create setting for placement of torrent files
                string filepath = Path.Combine(
                    Path.GetTempPath(),
                    Path.GetFileName(Url.AbsolutePath));
                
                using(var cli = new HttpClient()) {
                    var result = await cli.GetAsync(Url);
                    using(var fs = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None)){
                        await result.Content.CopyToAsync(fs);
                    }
                }

                Process.Start(filepath);
            }
            else // We don't know what this link is
                throw new TorrentException($"Unexpected filetype encountered while downloading {Name}"); 
        }
    }
}