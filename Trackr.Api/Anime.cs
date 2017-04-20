using System;

namespace Trackr.Api {
    /// <summary>
    /// Reperesents an anime item from an API call
    /// </summary>
    public class Anime : ApiEntry{

        /// <summary>
        /// Values representing the running status of the given anime.
        /// </summary>
        public enum RunningStatuses {
            Airing,
            Completed,
            NotYetAired
        }

        /// <summary>
        /// What type of media is the show?
        /// </summary>
        public enum ShowTypes {
            Tv,
            Ova,
            Ona,
            Movie,
            Special,
            Music
        }

        /// <summary>
        /// The day the series started airing.
        /// </summary>
        public DateTime StartDate { get; }
        /// <summary>
        /// The day the series finished airing.
        /// </summary>
        public DateTime EndDate { get; }

        /// <summary>
        /// The type of show
        /// </summary>
        public ShowTypes Type { get; }

        /// <summary>
        /// The running status of the show.
        /// </summary>
        public RunningStatuses Status { get; }

        /// <summary>
        /// The number of episodes aired.
        /// </summary>
        public int Episodes { get; }
        /// <summary>
        /// The last episode the user completed.
        /// </summary>
        public int CurrentEpisode { get; set; }

        internal Anime(int id, string title, string english,
            string[] synonyms, int episodes, double score,
            ShowTypes type, RunningStatuses status,
            DateTime start, DateTime end, string synopsis,
            string imageurl){

            Id = id;
            Title = title;
            EnglishTitle = english;
            Synonyms = synonyms;
            Episodes = episodes;
            Score = score;
            StartDate = start;
            EndDate = end;
            Type = type;
            Status = status;
            Synopsis = synopsis;
            ImageUrl = imageurl;
        }
    }
}