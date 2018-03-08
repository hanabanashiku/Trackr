using System;
using System.Linq;
using Gtk;
using Trackr.Api;

namespace Trackr.Gui.Gtk {
	internal class AnimeTreeView : TreeView {
		internal enum TreeColumns {Title, Episode, Progress, Score, Type, Season, NextEp};

		internal readonly ListStore Store;
		internal readonly TreeModelFilter Filter;
		private readonly TreeViewColumn _title, _episode, _progress, _score, _type, _season, _next;
		private readonly AnimeWindow _parent;
		
		internal AnimeTreeView(AnimeWindow parent) {
			_parent = parent;
			Store = new ListStore(typeof(Anime));
			Filter = new TreeModelFilter(Store, null) { VisibleFunc = FilterTree };
			Model = Filter;
			Store.SetSortFunc((int)TreeColumns.Title, CompareTitle); // sort by title by default
			
			// Create our columns
			_title = new TreeViewColumn() {
				Title = "Title",
				Resizable = true,
				Clickable = true,
				SortColumnId = (int)TreeColumns.Title
			};
			_title.Clicked += TitleClicked;
			_title.PackStart(new CellRendererText(), true);
			_title.SetCellDataFunc(_title.CellRenderers[0], RenderTitle);
			AppendColumn(_title);
			
			_episode = new TreeViewColumn() {
				Title = "Episode",
				Resizable = true,
				SortColumnId = (int)TreeColumns.Episode
			};
			_episode.PackStart(new CellRendererText(), true);
			_episode.SetCellDataFunc(_episode.CellRenderers[0], RenderEpisode);
			AppendColumn(_episode);
			
			_progress = new TreeViewColumn() { Title = "Progress", SortColumnId = (int)TreeColumns.Progress};
			_progress.PackStart(new CellRendererProgress(), true);
			_progress.SetCellDataFunc(_progress.CellRenderers[0], RenderProgress);
			AppendColumn(_progress);
			
			_score = new TreeViewColumn() {
				Title = "Score",
				Resizable = true,
				Clickable = true,
				SortColumnId = (int)TreeColumns.Score
			};
			_score.Clicked += ScoreClicked;
			_score.PackStart(new CellRendererText(), true);
			_score.SetCellDataFunc(_score.CellRenderers[0], RenderScore);
			AppendColumn(_score);
			
			_type = new TreeViewColumn() { 
				Title = "Type",
				Resizable = true,
				Clickable = true,
				SortColumnId = (int)TreeColumns.Type
			};
			_type.Clicked += TypeClicked;
			_type.PackStart(new CellRendererText(), true);
			_type.SetCellDataFunc(_type.CellRenderers[0], RenderType);
			AppendColumn(_type);
			
			_season = new TreeViewColumn() { 
				Title = "Season",
				Resizable = true,
				Clickable = true,
				SortColumnId = (int)TreeColumns.Season
			};
			_season.PackStart(new CellRendererText(), true);
			_season.SetCellDataFunc(_season.CellRenderers[0], RenderSeason);
			AppendColumn(_season);
			
			_next = new TreeViewColumn() { 
				Title = "Status",
				Resizable = true,
				Clickable = true,
				SortColumnId = (int)TreeColumns.NextEp
			};
			_next.PackStart(new CellRendererText(), true);
			_next.SetCellDataFunc(_next.CellRenderers[0], RenderNextEpisode);
			AppendColumn(_next);
		}

		private static void RenderTitle(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (Anime)m.GetValue(i, 0);
			switch(Program.Settings.TitleDisplay){
				case Core.Settings.TitleDisplays.English:
					((CellRendererText)cell).Text = a.EnglishTitle;
					break;
				case Core.Settings.TitleDisplays.Japanese:
					((CellRendererText)cell).Text = a.JapaneseTitle;
					break;
				case Core.Settings.TitleDisplays.Romaji:
					((CellRendererText)cell).Text = a.Title;
					break;
			}
		}
		
		private static int CompareTitle(TreeModel m, TreeIter i, TreeIter j) {
			var a = (Anime)m.GetValue(i, 0);
			var b = (Anime)m.GetValue(j, 0);
			return string.CompareOrdinal(a.Title, b.Title);
		}

		private void TitleClicked(object o, EventArgs args) {
			SetSortOrder(_title);
			_title.SortIndicator = true;
			Store.SetSortFunc((int)TreeColumns.Title, CompareTitle);
			Store.SetSortColumnId((int)TreeColumns.Title, _title.SortOrder);
		}

		private static void RenderEpisode(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (Anime)m.GetValue(i, 0);
			((CellRendererText)cell).Text = a.CurrentEpisode + "/" + a.Episodes;
		}

		private static void RenderProgress(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (Anime)m.GetValue(i, 0);
			var crp = (CellRendererProgress)cell;
			crp.Text = string.Empty;
			if(a.Episodes == 0) { // we don't know how many episodes
				if(a.AirTimes.Count > 0)
					crp.Value = (int)(a.CurrentEpisode / (decimal)a.AirTimes.Keys.Max() * 100); // use the last known episode
				else if(a.StartDate != DateTime.MinValue) { // Estimate based on one episode per week
					var elapsed = DateTime.Now - a.StartDate;
					var val = a.CurrentEpisode / (elapsed.TotalDays / 7);
					if(val >= 1) crp.Value = 75;
					else crp.Value = (int)(val * 100);
				}
				else crp.Value = 75;
			}
			else crp.Value = (int)(a.CurrentEpisode / (decimal)a.Episodes * 100); // we know how many episodes!
		}

		private static void RenderScore(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (Anime)m.GetValue(i, 0);
			((CellRendererText)cell).Text = a.UserScore.ToString();
		}
		
		private static int CompareScore(TreeModel m, TreeIter i, TreeIter j) {
			var a = (Anime)m.GetValue(i, 0);
			var b = (Anime)m.GetValue(j, 0);
			return a.UserScore.CompareTo(b.UserScore);
		}
		
		private void ScoreClicked(object o, EventArgs args) {
			SetSortOrder(_score);
			_score.SortIndicator = true;
			Store.SetSortFunc((int)TreeColumns.Score, CompareScore);
			Store.SetSortColumnId((int)TreeColumns.Score, _score.SortOrder);
		}

		private static void RenderType(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (Anime)m.GetValue(i, 0);
			var type = Enum.GetName(typeof(Anime.ShowTypes), a.Type);
			if(type?.Length < 4) type = type.ToUpper();
			((CellRendererText)cell).Text = type;
		}
		
		private static int CompareType(TreeModel m, TreeIter i, TreeIter j) {
			var a = (Anime)m.GetValue(i, 0);
			var b = (Anime)m.GetValue(j, 0);
			return ((int)b.Type).CompareTo((int)a.Type); // lower numbers first
		}
		
		private void TypeClicked(object o, EventArgs args) {
			SetSortOrder(_type);
			_type.SortIndicator = true;
			Store.SetSortFunc((int)TreeColumns.Type, CompareType);
			Store.SetSortColumnId((int)TreeColumns.Type, _type.SortOrder);
		}

		private static void RenderSeason(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			//TODO RenderSeason, CompareSeason, SeasonClicked
		}

		// Display countdown to episode release if the episode is not out yet.
		// Otherwise, say if the next episode is in the library folders, or if the next episode has been released
		private static void RenderNextEpisode(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (Anime)m.GetValue(i, 0);
			if(!a.AirTimes.ContainsKey(a.CurrentEpisode + 1)) return;
			
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
		// TODO CompareNextEpisode, NextEpisodeClicked

		private static void SetSortOrder(TreeViewColumn c) {
			if(c.SortIndicator)
				c.SortOrder = c.SortOrder == SortType.Ascending ? SortType.Descending : SortType.Ascending;
			else c.SortOrder = SortType.Ascending;
		}
		
		private bool FilterTree(TreeModel m, TreeIter i) {
			var filter = _parent.FilterEntry.Text;
			if(filter == string.Empty) return true;
			
			var a = (Anime)m.GetValue(i, 0);
			if(a.Title.Contains(filter)) return true;
			if(a.EnglishTitle.Contains(filter)) return true;
			if(a.JapaneseTitle.Contains(filter)) return true;
			if(a.Synopsis.Contains(filter)) return true;
			if(a.Synonyms.ToList().Exists(x => x.Contains(filter))) return true;
			return false;
		}
	}
}