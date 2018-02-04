using System;
using System.Linq;
using Gtk;
using Trackr.Api;

namespace Trackr.Gui.Gtk {
	internal class AnimeTreeView : TreeView {
		internal enum TreeColumns {Title, Episode, Progress, Score, Type, Season, NextEp};

		internal readonly ListStore Store;
		internal readonly TreeModelFilter Filter;

		private readonly AnimeWindow _parent;
		
		internal AnimeTreeView(AnimeWindow parent) {
			_parent = parent;
			Store = new ListStore(typeof(Anime));
			Filter = new TreeModelFilter(Store, null) { VisibleFunc = FilterTree };
			Model = Filter;
			
			// Create our columns
			var c = new TreeViewColumn() {
				Title = "Title",
				Resizable = true,
				SortColumnId = (int)TreeColumns.Episode,
				SortIndicator = true
			};
			c.PackStart(new CellRendererText(), true);
			c.SetCellDataFunc(c.CellRenderers[0], RenderTitle);
			AppendColumn(c);
			
			c = new TreeViewColumn() {
				Title = "Episode",
				Resizable = true,
				SortColumnId = (int)TreeColumns.Episode,
				SortIndicator = true
			};
			c.PackStart(new CellRendererText(), true);
			c.SetCellDataFunc(c.CellRenderers[0], RenderEpisode);
			AppendColumn(c);
			
			c = new TreeViewColumn() { Title = "Progress" };
			c.PackStart(new CellRendererProgress(), true);
			c.SetCellDataFunc(c.CellRenderers[0], RenderProgress);
			AppendColumn(c);
			
			c = new TreeViewColumn() {
				Title = "Score",
				Resizable = true,
				Clickable = true,
				SortColumnId = (int)TreeColumns.Score,
				SortIndicator = true
			};
			c.PackStart(new CellRendererText(), true);
			c.SetCellDataFunc(c.CellRenderers[0], RenderScore);
			AppendColumn(c);
			
			c = new TreeViewColumn() { 
				Title = "Type",
				Resizable = true,
				Clickable = true,
				SortColumnId = (int)TreeColumns.Type,
				SortIndicator = true
			};
			c.PackStart(new CellRendererText(), true);
			c.SetCellDataFunc(c.CellRenderers[0], RenderType);
			AppendColumn(c);
			
			c = new TreeViewColumn() { 
				Title = "Season",
				Resizable = true,
				Clickable = true,
				SortColumnId = (int)TreeColumns.Season,
				SortIndicator = true 
			};
			c.PackStart(new CellRendererText(), true);
			c.SetCellDataFunc(c.CellRenderers[0], RenderSeason);
			AppendColumn(c);
			
			c = new TreeViewColumn() { 
				Title = "Status",
				Resizable = true,
				Clickable = true,
				SortColumnId = (int)TreeColumns.Episode,
				SortIndicator = true 
			};
			c.PackStart(new CellRendererText(), true);
			c.SetCellDataFunc(c.CellRenderers[0], RenderNextEpisode);
			AppendColumn(c);
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

		private static void RenderEpisode(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (Anime)m.GetValue(i, 0);
			((CellRendererText)cell).Text = a.CurrentEpisode + "/" + a.Episodes;
		}

		private static void RenderProgress(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (Anime)m.GetValue(i, 0);
			var crp = (CellRendererProgress)cell;
			if(a.Episodes == 0) { // we don't know how many episodes
				if(a.AirTimes.Count > 0)
					crp.Value = (int)(a.CurrentEpisode / (decimal)a.AirTimes.Keys.Max() * 100); // use the last known episode
				else crp.Value = 75; // close enough
			}
			else crp.Value = (int)(a.CurrentEpisode / (decimal)a.Episodes * 100); // we know how many episodes!
		}

		private static void RenderScore(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (Anime)m.GetValue(i, 0);
			((CellRendererText)cell).Text = a.UserScore.ToString();
		}

		private static void RenderType(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			var a = (Anime)m.GetValue(i, 0);
			var type = Enum.GetName(typeof(Anime.ShowTypes), a.Type);
			if(type?.Length < 4) type = type.ToUpper();
			((CellRendererText)cell).Text = type;
		}

		private static void RenderSeason(TreeViewColumn c, CellRenderer cell, TreeModel m, TreeIter i) {
			//TODO
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