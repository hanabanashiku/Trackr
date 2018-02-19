using System;

namespace Trackr.Api {
	public class AnimeEpisode {
		/// <summary>
		/// The date the episode aired.
		/// </summary>
		public DateTime AirDate { get; }
		/// <summary>
		/// The episode number.
		/// </summary>
		public uint Number { get; }
		/// <summary>
		/// The season number.
		/// </summary>
		/// <remarks>0 if not defined.</remarks>
		public uint Season { get; }
		/// <summary>
		/// The English title.
		/// </summary>
		public string EnglishTitle { get; }
		/// <summary>
		/// The Japanese title.
		/// </summary>
		public string JapaneseTitle { get; }
		/// <summary>
		/// The episode synopsis
		/// </summary>
		public string Synopsis { get; }

		internal AnimeEpisode(DateTime airDate, uint number, uint season, string englishTitle, string japaneseTitle, string synopsis) {
			AirDate = airDate;
			Number = number;
			Season = season;
			EnglishTitle = englishTitle;
			JapaneseTitle = japaneseTitle;
			Synopsis = synopsis;
		}
	}
}