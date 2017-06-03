using System;

namespace Trackr.Torrent {
    public class TorrentException : Exception{
        public TorrentException(string msg) : base(msg) {}
    }
}