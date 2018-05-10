using System;
using Gtk;

using Trackr.Api;

namespace Trackr.Gui.Gtk {	
	// TODO genre 
	internal class AnimeSearchTreeView : MediaTreeView<Anime> {
		internal enum TreeColumns {
			Title,
			Episodes,
			Score,
			Type,
			Season,
			Genre,
			Status
		};
		
		internal new readonly ListStore Store;
		private readonly TreeViewColumn _title, _episodes, _score, _type, _season, _status, _genre;

		internal AnimeSearchTreeView() {
			Store = new ListStore(typeof(Anime));
			
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
			
			_episodes = new TreeViewColumn() {
				Title = "Episode",
				Resizable = true,
				Clickable = true,
				SortColumnId = (int)TreeColumns.Episodes
			};
			_episodes.Clicked += EpisodesClicked;
			_episodes.PackStart(new CellRendererText(), true);
			_episodes.SetCellDataFunc(_episodes.CellRenderers[0], RenderEpisodes);
			AppendColumn(_episodes);
			
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
			
			_status = new TreeViewColumn() { 
				Title = "Status",
				Resizable = true,
				Clickable = true,
				SortColumnId = (int)TreeColumns.Status
			};
			_status.Clicked += StatusClicked;
			_status.PackStart(new CellRendererText(), true);
			_status.SetCellDataFunc(_status.CellRenderers[0], RenderStatus);
			AppendColumn(_status);

			RowActivated += OnAnimeRowActivated;
		}
		
		private void TitleClicked(object o, EventArgs args) {
			SetSortOrder(_title);
			ResetIndicators();
			_title.SortIndicator = true;
			Store.SetSortFunc((int)TreeColumns.Title, CompareTitle);
			Store.SetSortColumnId((int)TreeColumns.Title, _title.SortOrder);
		}

		private void EpisodesClicked(object o, EventArgs args) {
			SetSortOrder(_episodes);
			ResetIndicators();
			_episodes.SortIndicator = true;
			Store.SetSortFunc((int)TreeColumns.Episodes, CompareEpisodes);
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
		
		private void StatusClicked(object o, EventArgs args) {
			SetSortOrder(_status);
			ResetIndicators();
			_status.SortIndicator = true;
			Store.SetSortFunc((int)TreeColumns.Status, CompareStatus);
			Store.SetSortColumnId((int)TreeColumns.Status, _status.SortOrder);
		}

		private void ResetIndicators() {
			_title.SortIndicator = false;
			_episodes.SortIndicator = false;
			_score.SortIndicator = false;
			_type.SortIndicator = false;
			_season.SortIndicator = false;
			_status.SortIndicator = false;
			_genre.SortIndicator = false;
		}
	}
}