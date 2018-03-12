﻿using System;
using System.Drawing;
 using System.IO;
 using System.Net.Http;
 using System.Threading.Tasks;

namespace Trackr.Api {
    /// <summary>
    /// Represents an item result from an API call
    /// </summary>
    [Serializable]
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
        /// The Japanese title of the item.
        /// </summary>
        public string JapaneseTitle { get; protected set; }
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

        protected int _id;
        /// <summary>
        /// The API's internal database ID.
        /// </summary>
        public int Id => _id;

        /// <summary>
        /// The title's user-generated score.
        /// </summary>
        public double Score { get; protected set; }

        /// <summary>
        /// The title's current status in the user's list.
        /// </summary>
        public ListStatuses ListStatus { get; set; } = ListStatuses.NotInList;

        /// <summary>
        /// The date at which the user first started the title.
        /// </summary>
        public DateTime UserStart { get; set; } = DateTime.MinValue;

        /// <summary>
        /// The date at which the user finished the title.
        /// </summary>
        public DateTime UserEnd { get; set; } = DateTime.MinValue;

        protected int _userScore;
        /// <summary>
        /// The user's personal score of the title, from 0 to 10.
        /// </summary>
        public int UserScore {
            get => _userScore;
            set {
                if(value < 0 || value > 10)
                    throw new ArgumentOutOfRangeException();
                _userScore = value;
            }
        }

        public async Task<Stream> GetCover() {
            using(var http = new HttpClient()) {
                var res = await http.GetAsync(ImageUrl);
                if(!res.IsSuccessStatusCode) throw new ApiRequestException(res.StatusCode.ToString());
                return await res.Content.ReadAsStreamAsync();
            }
        }
    }
}