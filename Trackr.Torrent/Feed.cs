using System;
using System.ServiceModel.Syndication;
using System.Xml;

namespace Trackr.Torrent {
    /// <summary>
    /// Represents an RSS feed for grabbing torrents.
    /// </summary>
    public class Feed {
        public string Name { get; set; }
        public string Url { get; }
        private SyndicationFeed _feed;

        public Feed(string url, string name = ""){
            Url = url;
            try {
                XmlReader xml = XmlReader.Create(Url);
                _feed = SyndicationFeed.Load(xml);
                if(_feed == null) throw new FeedException("Null feed");
            }
            catch(XmlException e) {
                throw new FeedException($"Error loading feed: {e.Message}");
            }

            if(name == "")
                Name = _feed.Title.Text != null ? name : "Feed";
            else Name = name;
            
        }
    }
}