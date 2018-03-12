using System;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Web.UI.WebControls.WebParts;

namespace Trackr.Api {
	/// <summary>
	/// An enum containing season information for an anime
	/// </summary>
	public struct Season {
		public enum Cour { Winter = 0, Spring = 1, Summer = 2, Fall = 3 }

		/// <summary>
		/// The quarter the show was released in
		/// </summary>
		public Cour Quarter;

		/// <summary>
		/// The year the show was released in
		/// </summary>
		public int Year;

		public Season(Anime a) {
			Quarter = GetCour(a.StartDate);
			Year = a.StartDate.Year;
		}

		public Season(Manga m) {
			Quarter = GetCour(m.StartDate);
			Year = m.StartDate.Year;
		}

		private static Cour GetCour(DateTime dt) {
			if(dt.Month < 4) return Cour.Winter;
			if(dt.Month < 6) return Cour.Spring;
			if(dt.Month < 9) return Cour.Summer;
			return Cour.Fall;
		}

		public override string ToString() {
			string season;
			switch(Quarter) {
					case Cour.Winter:
						season = "Winter";
						break;
					case Cour.Spring:
						season = "Spring";
						break;
					case Cour.Summer:
						season = "Summer";
						break;
					case Cour.Fall:
						season = "Fall";
						break;
					default:
						throw new InvalidEnumArgumentException();
			}
			return $"{season} {Year}";
		}
	}
}