using System;
using Trackr.api;

namespace Trackr.Api{
    /// <summary>
    /// Represents a manga item from an API call
    /// </summary>
    public class Manga : ApiEntry{
        /// <summary>
        /// Specifies the API-defined manga type
        /// </summary>
        public enum MangaTypes {
            Manga,
            Novel,
            OneShot,
            Doujinshi,
            Manhwa,
            Manhua
        }

        /// <summary>
        /// Values representing the running status of a manga.
        /// </summary>
        public enum RunningStatuses {
            Publishing,
            Finished,
            NotYetPublished
        }

        /// <summary>
        /// The day the series started publishing.
        /// </summary>
        public DateTime StartDate { get; }
        /// <summary>
        /// The day the series finished publishing.
        /// </summary>
        public DateTime EndDate { get; }
        /// <summary>
        /// The number of chapters in the manga.
        /// </summary>
        public int Chapters { get; }
        /// <summary>
        /// The number of manga volumes released.
        /// </summary>
        public int Volumes { get; }
        /// <summary>
        /// The current running status of the manga.
        /// </summary>
        public RunningStatuses Status { get; }
        /// <summary>
        /// The type of manga.
        /// </summary>
        public MangaTypes Type { get; }
        /// <summary>
        /// The last chapter the user completed.
        /// </summary>
        public int CurrentChapter { get; set; }
        /// <summary>
        /// The last volume the user completed.
        /// </summary>
        public int CurrentVolume { get; set; }

        /// <summary>
        /// The scan group that distributed the manga scans.
        /// </summary>
        public string ScanGroup { get; set; } = string.Empty;

        internal Manga(int id, string title, string english,
            string[] synonyms, int chapters, int volumes, double score,
            MangaTypes type, RunningStatuses status,
            DateTime start, DateTime end, string synopsis,
            string imageurl) {
            Id = id;
            Title = title;
            EnglishTitle = english;
            Synonyms = synonyms;
            Chapters = chapters;
            Volumes = volumes;
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
