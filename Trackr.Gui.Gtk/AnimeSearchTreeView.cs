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
			Status,
			ListStatus
		};
		
		private readonly TreeViewColumn _title, _episodes, _score, _type, _season, _status, _genre, _listStatus;

		internal AnimeSearchTreeView() {
			Store = new ListStore(typeof(Anime));
			Store.SetSortFunc((int)TreeColumns.Title, CompareTitle);
			Model = Store;
			
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
				Title = "Episodes",
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
			_score.SetCellDataFunc(_score.CellRenderers[0], RenderPublicScore);
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

			_genre = new TreeViewColumn() {
				Title = "Genre",
				Resizable = true,
				Clickable = true,
				SortColumnId = (int)TreeColumns.Genre
			};
			_genre.Clicked += GenreClicked;
			_genre.PackStart(new CellRendererText(), true);
			//_genre.SetCellDataFunc(_genre.CellRenderers[0], RenderGenre);
			AppendColumn(_genre);
			
			_listStatus = new TreeViewColumn() {
				Title = "List Status",
				Resizable = true,
				Clickable = true,
				SortColumnId = (int)TreeColumns.ListStatus
			};
			_listStatus.Clicked += ListStatusClicked;
			_listStatus.PackStart(new CellRendererText(), true);
			_listStatus.SetCellDataFunc(_listStatus.CellRenderers[0], RenderListStatus);
			AppendColumn(_listStatus);
			
			RowActivated += OnRowActivated;

			ShowAll();
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

		private void GenreClicked(object o, EventArgs args) {
			SetSortOrder(_genre);
			ResetIndicators();
			_genre.SortIndicator = true;
			//Store.SetSortFunc((int)TreeColumns.Genre, CompareGenre);
			Store.SetSortColumnId((int)TreeColumns.Genre, _genre.SortOrder);
		}

		private void ListStatusClicked(object o, EventArgs args) {
			SetSortOrder(_listStatus);
			ResetIndicators();
			_listStatus.SortIndicator = true;
			Store.SetSortFunc((int)TreeColumns.ListStatus, CompareListStatus);
			Store.SetSortColumnId((int)TreeColumns.ListStatus, _listStatus.SortOrder);
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

		protected override void OnRowActivated(object o, RowActivatedArgs args) {
			Store.GetIter(out var i, args.Path);
			var a = (Anime)Store.GetValue(i, 0);
			var d = new AnimeDialog(a);
			var response = (ResponseType)d.Run();
			var result = d.Result;
			d.Destroy();

			// We refill instead of doing this the more efficient way,
			// as to not deal with specific references. This is justifiable
			// since we are not looking at the list window right now, but the search one.
			switch(response) {
				// we're adding a new anime!
				case ResponseType.Accept:
					a.Replace(result);
					Program.AnimeList.Add(a);
					Program.Win.Fill(typeof(Anime));
					break;
				
				// we're changing values!
				case ResponseType.Apply:
					a.Replace(result);
					Program.AnimeList.Update(a);
					Program.Win.Fill(typeof(Anime));
					break;
				
				default: return; // We just want to cancel
			}
			
			Program.Win.RefreshAnimeLists();
		}
	}
}