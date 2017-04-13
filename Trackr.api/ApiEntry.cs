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
            Planned = 6
        };

        // set by api
        /// <summary>
        /// The title of the item.
        /// </summary>
        public string Title { get; protected set; }
        /// <summary>
        /// The English translated title of the item.
        /// </summary>
        public string EnglishTitle { get; protected set; }
        /// <summary>
        /// Synonyms of the title.
        /// </summary>
        public string[] Synonyms { get; protected set; }
        /// <summary>
        /// The synopsis of the title.
        /// </summary>
        public string Synopsis { get; protected set; }
        /// <summary>
        /// A URL pointing to the title's cover image.
        /// </summary>
        public string ImageUrl { get; protected set; }
        /// <summary>
        /// The API's internal database ID.
        /// </summary>
        public int Id { get; protected set; }
        /// <summary>
        /// The title's user-generated score.
        /// </summary>
        public double Score { get; protected set; }

        /// <summary>
        /// The title's current status in the user's list.
        /// </summary>
        public ListStatuses ListStatus { get; set; }
        /// <summary>
        /// The date at which the user first started the title.
        /// </summary>
        public DateTime UserStart { get; set; }
        /// <summary>
        /// The date at which the user finished the title.
        /// </summary>
        public DateTime UserEnd { get; set; }

        /// <summary>
        /// The user's personal score of the title.
        /// </summary>
        public int UserScore {
            get { return _userScore; }
            set {
                if(value < 0 || value > 10)
                    throw new ArgumentOutOfRangeException();
                _userScore = value;
            }
        }

        public Image Cover => Image.FromFile(ImageUrl);
            protected int _userScore;
        }
}