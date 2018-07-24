using System;
using System.Diagnostics;
using System.Linq;
using Gtk;
using Trackr.Api;

namespace Trackr.Gui.Gtk {
	internal class AnimeTreeView : MediaTreeView<Anime> {
		internal enum TreeColumns {Title, Episode, Progress, Score, Type, Season, NextEp};

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
				Clickable = true,
				SortColumnId = (int)TreeColumns.Episode
			};
			_episode.Clicked += EpisodeClicked;
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
			_type.SetCellDataFunc(_type.CellRenderers[0], RenderAnimeType);
			AppendColumn(_type);
			
			_season = new TreeViewColumn() { 
				Title = "Season",
				Resizable = true,
				Clickable = true,
				SortColumnId = (int)TreeColumns.Season
			};
			_season.Clicked += SeasonClicked;
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

			RowActivated += OnRowActivated;
		}
		

		private void TitleClicked(object o, EventArgs args) {
			SetSortOrder(_title);
			ResetIndicators();
			_title.SortIndicator = true;
			Store.SetSortFunc((int)TreeColumns.Title, CompareTitle);
			Store.SetSortColumnId((int)TreeColumns.Title, _title.SortOrder);
		}

		private void EpisodeClicked(object o, EventArgs args) {
			SetSortOrder(_episode);
			ResetIndicators();
			_episode.SortIndicator = true;
			Store.SetSortFunc((int)TreeColumns.Episode, CompareEpisode);
			Store.SetSortColumnId((int)TreeColumns.Episode, _episode.SortOrder);
		}
		
		private void ScoreClicked(object o, EventArgs args) {
			SetSortOrder(_score);
			ResetIndicators();
			_score.SortIndicator = true;
			Store.SetSortFunc((int)TreeColumns.Score, CompareScore);
			Store.SetSortColumnId((int)TreeColumns.Score, _score.SortOrder);
		}
		
		private void TypeClicked(object o, EventArgs args) {
			SetSortOrder(_type);
			ResetIndicators();
			_type.SortIndicator = true;
			Store.SetSortFunc((int)TreeColumns.Type, CompareAnimeType);
			Store.SetSortColumnId((int)TreeColumns.Type, _type.SortOrder);
		}

		private void SeasonClicked(object o, EventArgs args) {
			SetSortOrder(_season);
			ResetIndicators();
			_season.SortIndicator = true;
			Store.SetSortFunc((int)TreeColumns.Season, CompareSeason);
			Store.SetSortColumnId((int)TreeColumns.Season, _season.SortOrder);
		}
		
		// TODO CompareNextEpisode, NextEpisodeClicked

		private void ResetIndicators() {
			_title.SortIndicator = false;
			_episode.SortIndicator = false;
			_progress.SortIndicator = false;
			_score.SortIndicator = false;
			_type.SortIndicator = false;
			_season.SortIndicator = false;
			_next.SortIndicator = false;
		}
		
		private bool FilterTree(TreeModel m, TreeIter i) {
			var filter = _parent.FilterEntry.Text.ToLower();
			if(filter == string.Empty) return true;
			
			var a = (Anime)m.GetValue(i, 0);
			if(a.Title.ToLower().Contains(filter)) return true;
			if(a.EnglishTitle.ToLower().Contains(filter)) return true;
			if(a.JapaneseTitle.Contains(filter)) return true;
			if(a.Synopsis.ToLower().Contains(filter)) return true;
			if(a.Synonyms.ToList().Exists(x => x.ToLower().Contains(filter))) return true;
			return false;
		}
		
		protected override void OnRowActivated(object o, RowActivatedArgs args) {
			Store.GetIter(out var i, args.Path);
			var a = (Anime)Store.GetValue(i, 0); // original
			var d = new AnimeDialog(a);
			var response = (ResponseType)d.Run();
			var result = d.Result; // new version
			d.Destroy();

			switch(response) {
				// We activated a row from the list! No way we could be adding it!
				case ResponseType.Accept:
					Debug.WriteLine("Adding an anime that already exists!");
					break;

				// We're changing values!
				case ResponseType.Apply:
					// Deleteing from list...
					if(result.ListStatus == ApiEntry.ListStatuses.NotInList) {
						Program.AnimeList.Remove(a);
						Store.Remove(ref i);
						return;
					}

					// We have to move it to a different list!
					if(a.ListStatus != result.ListStatus) {
						a.Replace(result);
						Store.Remove(ref i);
						Program.Win.AnimeBox.Views[(int)a.ListStatus].Store.AppendValues(a);
					}
					// just update the values
					else a.Replace(result);

					Program.AnimeList.Update(a);
					Program.Win.RefreshAnimeLists();
					break;

				default: return; // We just want to cancel
			}
		}
	}
}