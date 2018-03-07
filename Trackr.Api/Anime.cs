﻿using System;
using System.Collections.Generic;

namespace Trackr.Api {
    /// <summary>
    /// Reperesents an anime item from an API call
    /// </summary>
    [Serializable]
    public class Anime : ApiEntry, IEquatable<Anime>{

        /// <summary>
        /// The API that grabbed the information.
        /// </summary>
        public string Provider {get; }

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
            Movie,
            Ova,
            Ona,
            Special,
            Music
        }

        /// <summary>
        /// The day the series started airing.
        /// </summary>
        public DateTime StartDate { get; private set; }
        /// <summary>
        /// The day the series finished airing.
        /// </summary>
        public DateTime EndDate { get; private set; }

        /// <summary>
        /// The type of show
        /// </summary>
        public ShowTypes Type { get; private set; }

        /// <summary>
        /// The running status of the show.
        /// </summary>
        public RunningStatuses Status { get; private set; }

        /// <summary>
        /// The number of episodes aired.
        /// </summary>
        public int Episodes { get; private set; }

        public Dictionary<int, DateTime> AirTimes;

        /// <summary>
        /// The last episode the user completed.
        /// </summary>
        public int CurrentEpisode { get; set; } // defualts to 0

        /// <summary>
        /// The user's series notes.
        /// </summary>
        public string Notes {get; set; } = string.Empty;

        internal Anime(int id, string title, string english,
            string japanese, string[] synonyms, int episodes,
            Dictionary<int, DateTime> airtimes, 
            double score, ShowTypes type, RunningStatuses status,
            DateTime start, DateTime end, string synopsis,
            string imageurl, string provider){
            _id = id;
            Title = title;
            JapaneseTitle = japanese;
            EnglishTitle = english;
            Synonyms = synonyms;
            Episodes = episodes;
            AirTimes = airtimes;
            Score = score;
            StartDate = start;
            EndDate = end;
            Type = type;
            Status = status;
            Synopsis = synopsis;
            ImageUrl = imageurl;
            Provider = provider;
        }

        /// <summary>
        /// Copy a reference over to this one
        /// </summary>
        /// <param name="a">The anime to shallow copy</param>
        /// <exception cref="ArgumentException">if the IDs or providers don't match</exception>
        public void Replace(Anime a) {
            if(a.Id != _id || a.Provider != Provider) throw new ArgumentException("The IDs and providers must be the same!");
            
            Title = a.Title;
            JapaneseTitle = a.JapaneseTitle;
            EnglishTitle = a.EnglishTitle;
            Synonyms = a.Synonyms;
            Episodes = a.Episodes;
            AirTimes = a.AirTimes;
            Score = a.Score;
            StartDate = a.StartDate;
            EndDate = a.EndDate;
            Type = a.Type;
            Status = a.Status;
            Synopsis = a.Synopsis;
            ImageUrl = a.ImageUrl;

            ListStatus = a.ListStatus;
            CurrentEpisode = a.CurrentEpisode;
            Notes = a.Notes;
            UserStart = a.UserStart;
            UserEnd = a.UserEnd;
            UserScore = a.UserScore;
        }

        /// <summary>
        /// Increase the episode count by one.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">If the user has already watched all episodes.</exception>
        public static Anime operator ++(Anime a){
            if(a.CurrentEpisode == a.Episodes)
                throw new IndexOutOfRangeException("The anime doesn't have that many episodes.");
            a.CurrentEpisode++;
            return a;
        }
        /// <summary>
        /// Decrease the episode count by one.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">If current episode is set to 0.</exception>
        public static Anime operator --(Anime a){
            if(a.CurrentEpisode == 0)
                throw new IndexOutOfRangeException("The episode count can't be less than 0.");
            a.CurrentEpisode--;
            return a;
        }

        /// <remarks>
        /// This is used to check for user values.
        /// </remarks>
        public static bool operator ==(Anime a, Anime b){
            if((object)a == null || (object)b == null) return false;
            if(a.Id != b.Id) return false;
            if(a.ListStatus != b.ListStatus) return false;
            if(a.CurrentEpisode != b.CurrentEpisode) return false;
            if(a.UserScore != b.UserScore) return false;
            if(a.UserStart != b.UserStart) return false;
            return !(a.UserEnd != b.UserEnd);
        }
        public static bool operator !=(Anime a, Anime b){
            return !(a == b);
        }

        public override bool Equals(object o) {
            if(o?.GetType() != typeof(Anime)) return false;
            var a = o as Anime;
            return a != null && Equals(a);
        }

        public new static bool Equals(object a, object b){
            if(a == null || b == null) return false;
            if(a.GetType() != typeof(Anime) || b.GetType() != typeof(Anime))
                return false;
            return ((Anime) a).Equals((Anime) b);
        }

        public bool Equals(Anime a){
            return a != null && a.Id == Id;
        }

        public override int GetHashCode(){
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return _id.GetHashCode();
        }
    }
}