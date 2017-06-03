using System;

namespace Trackr.Torrent {
    public class FeedException : Exception{
        public FeedException(string msg) : base(msg) {}
    }
}