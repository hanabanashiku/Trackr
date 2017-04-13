using System;

namespace Trackr.api {
    /// <summary>
    /// Reperesents an anime item from an API call
    /// </summary>
    public class Anime : ApiEntry{

        /// <summary>
        /// The current running status of the given anime.
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

        public DateTime StartDate { get; }

        public DateTime EndDate { get; }

        /// <summary>
        /// The type of show
        /// </summary>
        public ShowTypes Type { get; }

        /// <summary>
        /// The running status of the show.
        /// </summary>
        public RunningStatuses Status { get; }

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