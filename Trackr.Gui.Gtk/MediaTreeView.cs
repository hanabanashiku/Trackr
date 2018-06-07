using System;
using System.Globalization;
using System.Linq;
using Gtk;

using Trackr.Api;

namespace Trackr.Gui.Gtk {
	internal abstract class MediaTreeView<T> : TreeView where T : ApiEntry {
		internal ListStore Store;

		protected abstract void OnRowActivated(object o, RowActivatedArgs args);
		
		protected static void RenderTitle(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (ApiEntry)m.GetValue(i, 0);
			((CellRendererText)cell).Text = Program.GetTitle(a);
		}
		
		protected static int CompareTitle(TreeModel m, TreeIter i, TreeIter j) {
			var a = (ApiEntry)m.GetValue(i, 0);
			var b = (ApiEntry)m.GetValue(j, 0);
			return string.CompareOrdinal(a.Title, b.Title);
		}
		
		protected static void RenderEpisode(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (Anime)m.GetValue(i, 0);
			((CellRendererText)cell).Text = a.CurrentEpisode + "/" + a.Episodes;
		}

		// TODO Sort percernt complete instead
		protected static int CompareEpisode(TreeModel m, TreeIter i, TreeIter j) {
			var a = (Anime)m.GetValue(i, 0);
			var b = (Anime)m.GetValue(j, 0);
			return a.CurrentEpisode.CompareTo(b.CurrentEpisode);
		}
		
		protected static void RenderEpisodes(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (Anime)m.GetValue(i, 0);
			((CellRendererText)cell).Text =  a.Episodes.ToString();
		}
		
		protected static int CompareEpisodes(TreeModel m, TreeIter i, TreeIter j) {
			var a = (Anime)m.GetValue(i, 0);
			var b = (Anime)m.GetValue(j, 0);
			return a.Episodes.CompareTo(b.Episodes);
		}
		
		protected static void RenderProgress(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var crp = (CellRendererProgress)cell;
			crp.Text = string.Empty;

			if(typeof(T) == typeof(Anime)) {
				var a = (Anime)m.GetValue(i, 0);
				if(a.Episodes == 0) {
					// we don't know how many episodes
					// TODO Make sure its not null
					if(a.AirTimes != null && a.AirTimes.Count > 0)
						crp.Value = (int)(a.CurrentEpisode / (decimal)a.AirTimes.Keys.Max() * 100); // use the last known episode
					else if(a.StartDate != DateTime.MinValue) {
						// Estimate based on one episode per week
						var elapsed = DateTime.Now - a.StartDate;
						var val = a.CurrentEpisode / (elapsed.TotalDays / 7);
						if(val >= 1) crp.Value = 75;
						else crp.Value = (int)(val * 100);
					}
					else crp.Value = 75;
				}
				else crp.Value = (int)(a.CurrentEpisode / (decimal)a.Episodes * 100); // we know how many episodes!
			}

			else if(typeof(T) == typeof(Manga)) {
				var manga = (Manga)m.GetValue(i, 0);
				if(manga.Chapters == 0) {
					if(manga.Volumes != 0 && manga.CurrentVolume != 0 && manga.CurrentChapter != 0)
						crp.Value = (int)(manga.CurrentVolume / (decimal)manga.Volumes);
					else crp.Value = 75;
				}
				else crp.Value = (int)(manga.CurrentChapter / (decimal)manga.Chapters * 100);
			}
		}
		
		protected static void RenderScore(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (ApiEntry)m.GetValue(i, 0);
			((CellRendererText)cell).Text = a.UserScore.ToString();
		}

		protected static void RenderPublicScore(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (ApiEntry)m.GetValue(i, 0);
			((CellRendererText)cell).Text = a.Score.ToString(CultureInfo.InvariantCulture);
		}
		
		protected static int CompareScore(TreeModel m, TreeIter i, TreeIter j) {
			var a = (ApiEntry)m.GetValue(i, 0);
			var b = (ApiEntry)m.GetValue(j, 0);
			return a.UserScore.CompareTo(b.UserScore);
		}
		
		protected static void RenderAnimeType(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (Anime)m.GetValue(i, 0);
			var type = Enum.GetName(typeof(Anime.ShowTypes), a.Type);
			if(type?.Length < 4) type = type.ToUpper();
			((CellRendererText)cell).Text = type;
		}
		
		protected static int CompareAnimeType(TreeModel m, TreeIter i, TreeIter j) {
			var a = (Anime)m.GetValue(i, 0);
			var b = (Anime)m.GetValue(j, 0);
			return ((int)b.Type).CompareTo((int)a.Type); // lower numbers first
		}
		
		protected static void RenderMangaType(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (Manga)m.GetValue(i, 0);
			var type = Enum.GetName(typeof(Manga.MangaTypes), a.Type);
			if(type?.Length < 4) type = type.ToUpper();
			((CellRendererText)cell).Text = type;
		}
		
		protected static int CompareMangaType(TreeModel m, TreeIter i, TreeIter j) {
			var a = (Manga)m.GetValue(i, 0);
			var b = (Manga)m.GetValue(j, 0);
			return ((int)b.Type).CompareTo((int)a.Type); // lower numbers first
		}
		
		protected static void RenderSeason(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (Anime)m.GetValue(i, 0);
			((CellRendererText)cell).Text = a.Season.ToString();
		}

		protected static int CompareSeason(TreeModel m, TreeIter i, TreeIter j) {
			var a = (Anime)m.GetValue(i, 0);
			var b = (Anime)m.GetValue(j, 0);
			if(a.StartDate == DateTime.MinValue) return -1;
			return a.StartDate.CompareTo(b.StartDate);
		}

		protected static void RenderStatus(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var crt = (CellRendererText)cell;
			var a = (ApiEntry)m.GetValue(i, 0);

			if(typeof(T) == typeof(Anime))
				crt.Text = Enum.GetName(typeof(Anime.RunningStatuses), ((Anime)a).Status);
			else if(typeof(T) == typeof(Manga))
				crt.Text = Enum.GetName(typeof(Manga.RunningStatuses), ((Manga)a).Status);
		}

		protected static int CompareStatus(TreeModel m, TreeIter i, TreeIter j) {
			if(typeof(T) == typeof(Anime)) {
				var a = (Anime)m.GetValue(i, 0);
				var b = (Anime)m.GetValue(j, 0);
				return ((int)a.Status).CompareTo((int)b.Status);
			}
			if(typeof(T) == typeof(Manga)) {
				var a = (Manga)m.GetValue(i, 0);
				var b = (Manga)m.GetValue(j, 0);
				return ((int)a.Status).CompareTo((int)b.Status);
			}
			return 0;
		}

		protected static void RenderListStatus(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var crt = (CellRendererText)cell;
			var a = (ApiEntry)m.GetValue(i, 0);
			crt.Text = Enum.GetName(typeof(ApiEntry.ListStatuses), a.ListStatus);
		}

		protected static int CompareListStatus(TreeModel m, TreeIter i, TreeIter j) {
			var a = (ApiEntry)m.GetValue(i, 0);
			var b = (ApiEntry)m.GetValue(j, 0);
			return ((int)a.ListStatus).CompareTo((int)b.ListStatus);
		}

		protected static void RenderGenre(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			throw new NotImplementedException();
		}
		
		protected static int CompareGenre(TreeModel m, TreeIter i, TreeIter j) {
			throw new NotImplementedException();
		}

		// Display countdown to episode release if the episode is not out yet.
		// Otherwise, say if the next episode is in the library folders, or if the next episode has been released
		protected static void RenderNextEpisode(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (Anime)m.GetValue(i, 0);
			if(a.AirTimes == null || !a.AirTimes.ContainsKey(a.CurrentEpisode + 1)) return;
			
			var dt = a.AirTimes[a.CurrentEpisode + 1];
			// TODO: if currently in library folders
			if(DateTime.Today < dt) {
				((CellRendererText)cell).Foreground = "red";
				var ts = dt - DateTime.Today;
				((CellRendererText)cell).Text = "Next episode in " + ts.Hours + "h " + ts.Minutes + "m";
			}
			else {
				var crt = (CellRendererText)cell;
				crt.Foreground = "green";
				crt.Text = "Next episode released";
			}	
		}
		
		protected static void SetSortOrder(TreeViewColumn c) {
			if(c.SortIndicator)
				c.SortOrder = c.SortOrder == SortType.Ascending ? SortType.Descending : SortType.Ascending;
			else c.SortOrder = SortType.Ascending;
		}
	}
}