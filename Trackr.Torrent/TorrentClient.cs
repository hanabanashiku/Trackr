using System;
using System.Collections.Generic;

namespace Trackr.Torrent {
	/// <summary>
	/// An object for managing torrent feeds.
	/// </summary>
	public class TorrentClient : List<Feed> {

		private List<Feed> _feeds;

		public void SyncAll(){
			foreach(Feed f in this)
				f.Sync();
		}
	}
}

