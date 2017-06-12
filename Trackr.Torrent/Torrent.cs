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

		/// <summary>
		/// Name of the SubGroup that released the torrent.
		/// </summary>
		public string SubGroup { get; private set; }

		/// <summary>
		/// The name of the anime
		/// </summary>
		public string Anime { get; private set; }

		/// <summary>
		/// The episode number, or -1 for batch, -2 for error.
		/// </summary>
		public int Episode { get; private set; }

		/// <summary>
		/// The episode quality (480p, 1080p, etc.), or -1 for none given/not parsable.
		/// </summary>
		public int Quality { get; private set; }

		internal Torrent(SyndicationItem item) {
			Url = item.Links[0].Uri;
			Name = item.Title.Text;
			ParseTitle();
		}

		private void ParseTitle(){
			// [SubGroup] Anime Title - 01 [720p].mp4
			// TODO: support for batches
			Regex r = new Regex(@"\[([a-zA-Z-_]+)\]\s*(.+)\s*-\s*([0-9]+)\s*[[(](.+)[)\]](\.[a-zA-Z0-9]{3})?");
			Match m = r.Match(Name);

			// couldn't parse the torrent, incorrect format
			if(!m.Success) { 
				SubGroup = string.Empty;
				Anime = string.Empty;
				Episode = -2;
				Quality = -1;
				return;
			}

			SubGroup = m.Groups[0].Value;
			Anime = m.Groups[1].Value;
			Episode = int.Parse(m.Groups[2].Value);
			if(m.Groups.Count == 3)
				Quality = ParseQuality(m.Groups[3].Value);
			else Quality = -1; // no quality information given.
		}

		private int ParseQuality(string q){
			Regex r;
			Match m;

			// look for shorthand resolutions, e.g. 480p 
			r = new Regex(@"(\d+)p|i");
			m = r.Match(q);
			if(m.Success)
				return int.Parse(m.Groups[0].Value);
			// look for full resolutions, e.g. 640x480
			r = new Regex(@"\d+\s*x\s*(\d+)");
			m = r.Match(q);
			if(m.Success)
				return int.Parse(m.Groups[0].Value);

			// likely not a resolution box.
			// TODO: Currently, resolutions will only be found if it is in the first set of brackets.
			return -1;
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
						using(var fs = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None)) {
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