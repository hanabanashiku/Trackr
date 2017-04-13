using System;
using System.Drawing;

namespace Trackr.api {
    /// <summary>
    /// Represents an item result from an API call
    /// </summary>
    public abstract class ApiEntry {

        /// <summary>
        /// The List Status of the given entry
        /// </summary>
        public enum ListStatuses {
            NotInList = 0,
            Current = 1,
            Completed = 2,
            OnHold = 3,
            Dropped = 4,
            Planed = 6
        };

        // set by api
        public string Title { get; protected set; }
        public string EnglishTitle { get; protected set; }
        public string[] Synonyms { get; protected set; }
        public string Synopsis { get; protected set; }
        public string ImageUrl { get; protected set; }
        public int Id { get; protected set; }
        public double Score { get; protected set; }
        public int Episodes { get; protected set; }

        // user specific
        public int CurrentEpisode { get; set; }
        public ListStatuses ListStatus { get; set; }

        public Image Cover => Image.FromFile(ImageUrl);
    }
}