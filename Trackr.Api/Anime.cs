﻿using System;

namespace Trackr.Api {
    /// <summary>
    /// Reperesents an anime item from an API call
    /// </summary>
    public class Anime : ApiEntry, IEquatable<Anime>{

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

            _id = id;
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
            if(a == null || b == null) return false;
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

        public override bool Equals(object o){
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