using System;
using System.Globalization;
using Gtk;

using Trackr.Api;

namespace Trackr.Gui.Gtk {
	public class AnimeDialog : MediaDialog<Anime> {

		private SpinButton _episodeSpin, _scoreSpin;
		private DatePicker _userStart, _userEnd;
		private TextView _notesEntry;
		
		public AnimeDialog(Anime a) : base(a) {
			Result = new Anime(a);

			Build();
			ShowAll();
		}

		private void Build() {
			BuildHeader();
			BuildListWindow();
			BuildViewWindow();
		}

		private void BuildHeader() {
			var max = Original.Episodes == 0 ? double.MaxValue : Original.Episodes;
			var adj = new Adjustment(Original.CurrentEpisode, 0, max, 1, 10, 0);
			_episodeSpin = new SpinButton(adj, 1, 0);
			
			// Pack Episode Spinner
			EpisodeCounter.PackStart(new Label("Episode"), false, false, 20);
			EpisodeCounter.Add(_episodeSpin);
			_episodeSpin.SetSizeRequest(50, 22);
			_episodeSpin.ValueChanged += OnEpisodeChanged;
			EpisodeCounter.PackEnd(new VBox(), true, true, 100); // push everything left
			
			// Pack header table
			HeaderTable.NRows = 3;
			HeaderTable.NColumns = 2;
			HeaderTable.Attach(new Label("Type: \t"), 0, 1, 0, 1, AttachOptions.Shrink, AttachOptions.Expand, 0, 0);
			HeaderTable.Attach(new Label(Enum.GetName(typeof(Anime.ShowTypes), Original.Type)), 1, 2, 0, 1, AttachOptions.Shrink, AttachOptions.Expand, 0, 0);
			HeaderTable.Attach(new Label("Season: \t\t"), 0, 1, 1, 2, AttachOptions.Shrink, AttachOptions.Expand, 0, 0);
			HeaderTable.Attach(new Label(Original.Season.ToString()), 1, 2, 1, 2, AttachOptions.Shrink, AttachOptions.Expand, 0, 0);
			HeaderTable.Attach(new Label("Score: \t"), 0, 1, 2, 3, AttachOptions.Shrink, AttachOptions.Expand, 0, 0);
			HeaderTable.Attach(new Label(Original.Score.ToString(CultureInfo.InvariantCulture)), 1, 2, 2, 3, AttachOptions.Shrink, AttachOptions.Expand, 0, 0);
			
		}

		private void BuildListWindow() {
			_scoreSpin = new SpinButton(new Adjustment(Original.UserScore, 0, 10, 1, 10, 0), 1, 0);
			_userStart = new DatePicker(Original.UserStart);
			_userEnd = new DatePicker(Original.UserEnd);
			_notesEntry = new TextView() {Buffer = {Text = Original.Notes}};
			
			// Pack List Table
			ListTable.NRows = 5;
			ListTable.NColumns = 4;
			ListTable.Attach(new Label("Score"), 0, 1, 1, 2);
			ListTable.Attach(_scoreSpin, 2, 3, 1, 2);
			ListTable.Attach(new Label("Started At"), 0, 1, 2, 3);
			ListTable.Attach(_userStart, 2, 4, 2, 3);
			ListTable.Attach(new Label("Completed At"), 0, 1, 3, 4 );
			ListTable.Attach(_userEnd, 2, 4, 3, 4);
			ListTable.Attach(new Label("Notes"), 0, 1, 4, 5);
			ListTable.Attach(new ScrolledWindow(){new Viewport(){_notesEntry}}, 1, 3, 4, 6);

			// Events
			_scoreSpin.Changed += OnScoreChanged;
			_userStart.Changed += OnStartDateChanged;
			_userEnd.Changed += OnEndDateChanged;
			_notesEntry.Buffer.Changed += OnNotesChanged;
		}

		private void BuildViewWindow() {
			// TODO
		}

		private void OnEpisodeChanged(object o, EventArgs args) {
			Result.CurrentEpisode = _episodeSpin.ValueAsInt;
			if(Result.CurrentEpisode == Result.Episodes) {
				StatusBox.Active = ((int)ApiEntry.ListStatuses.Completed) - 1;
			}
			Changed = true;
		}

		private new void OnStatusChanged(object o, EventArgs args) {
			Result.ListStatus = (ApiEntry.ListStatuses)(StatusBox.Active + 1);
			if(Result.ListStatus == ApiEntry.ListStatuses.Completed) {
				_episodeSpin.Value = Result.Episodes;
				_userEnd.Value = DateTime.Today;
			}
			Changed = true;
		}

		private void OnScoreChanged(object o, EventArgs args) {
			Result.UserScore = _scoreSpin.ValueAsInt;
			Changed = true;
		}
		
		private void OnStartDateChanged(object o, EventArgs args) {
			Result.UserStart = _userStart.Value;
			Changed = true;
		}

		private void OnEndDateChanged(object o, EventArgs args) {
			Result.UserEnd = _userEnd.Value;
			Changed = true;
		}

		private void OnNotesChanged(object o, EventArgs args) {
			Result.Notes = _notesEntry.Buffer.Text;
			Changed = true;
		}
	}
}