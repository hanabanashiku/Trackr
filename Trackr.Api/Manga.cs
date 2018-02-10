﻿using System;

namespace Trackr.Api{
    /// <summary>
    /// Represents a manga item from an API call
    /// </summary>
    public class Manga : ApiEntry, IEquatable<Manga> {
        /// <summary>
        /// Specifies the API-defined manga type
        /// </summary>
        public enum MangaTypes {
            Manga,
            Novel,
            OneShot,
            Doujinshi,
            Manhwa,
            Manhua,
            Comic
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
        public int CurrentChapter { get; set; } = 0;

        /// <summary>
        /// The last volume the user completed.
        /// </summary>
        public int CurrentVolume { get; set; } = 0;
        
        /// <summary>
        /// The user's series notes.
        /// </summary>
        public string Notes {get; set; } = string.Empty;

        internal Manga(int id, string title, string english,
            string japanese, string[] synonyms, int chapters, int volumes,
            double score, MangaTypes type, RunningStatuses status,
            DateTime start, DateTime end, string synopsis,
            string imageurl) {
            _id = id;
            Title = title;
            EnglishTitle = english;
            JapaneseTitle = japanese;
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

        /// <remarks>
        /// This is used to check for user values.
        /// </remarks>
        public static bool operator ==(Manga a, Manga b){
            if((object)a == null || (object)b == null) return false;
            if(a.Id != b.Id) return false;
            if(a.ListStatus != b.ListStatus) return false;
            if(a.CurrentChapter != b.CurrentChapter) return false;
            if(a.CurrentVolume != b.CurrentVolume) return false;
            if(a.UserScore != b.UserScore) return false;
            if(a.UserStart != b.UserStart) return false;
            return !(a.UserEnd != b.UserEnd);
        }
        public static bool operator !=(Manga a, Manga b){
            return !(a == b);
        }

        public override bool Equals(object o){
            var m = o as Manga;
            return m != null && Equals(m);
        }

        public new static bool Equals(object a, object b){
            if(a == null || b == null) return false;
            if(a.GetType() != typeof(Manga) || b.GetType() != typeof(Manga))
                return false;
            return ((Manga) a).Equals((Manga) b);
        }

        public bool Equals(Manga m){
            return m != null && m.Id == Id;
        }

        public override int GetHashCode(){
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return _id.GetHashCode();
        }
    }
}
