using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

namespace Trackr.Torrent {
	/// <summary>
	/// Represents an RSS feed for grabbing torrents.
	/// </summary>
	public class Feed : IEnumerable<Torrent> {
		/// <summary>
		/// The name of the feed.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The URL of the feed.
		/// </summary>
		public string Url { get; }
		
		private List<Torrent> torrents;

		public Feed(string url, string name = ""){
			Url = url;
			if(name == "")
				Name = FetchFeed().Title.Text != null ? name : "Feed";
			else Name = name;
			Sync();
		}

		/// <summary>
		/// Sync this feed instance with the web server and pull all new torrents.
		/// </summary>
		public void Sync(){
			var feed = FetchFeed();
			if(feed == null) throw new FeedException("Null feed encountered.");
			torrents = feed.Items.Select(i => new Torrent(i)).ToList();
		}

		/// <summary>
		/// Get all torrents from the specified series.
		/// </summary>
		/// <param name="title">The series title, with any and all acronyms.</param>
		public List<Torrent> Get(params string[] title){
			return torrents.Where(x => title.Contains(x.Anime)).ToList();
		}

		public List<Torrent> Get(int quality, params string[] title){
			return torrents
				.Where(x => title.Contains(x.Anime))
				.Where(x => x.Quality == quality).ToList();
		}

		private SyndicationFeed FetchFeed(){
			try {
				XmlReader xml = XmlReader.Create(Url);
				return SyndicationFeed.Load(xml);
			} catch(XmlException e) { // this might need some changing later
				throw new TorrentException("Invalid feed parsed: " + e.Message);
			}
		}

		IEnumerator IEnumerable.GetEnumerator(){
			return GetEnumerator();
		}

		public IEnumerator<Torrent> GetEnumerator(){
			return torrents.GetEnumerator();
		}
	}
}