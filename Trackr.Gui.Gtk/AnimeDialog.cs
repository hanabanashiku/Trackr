﻿using System;
using System.Diagnostics;
using Gdk;
using Gtk;
using Pango;
using Trackr.Api;
using Image = Gtk.Image;

namespace Trackr.Gui.Gtk {
	/// <summary>
	/// A dialog for displaying an anime.
	/// </summary>
	/// <remarks>Returns Accept if creating a new anime, Apply if editing, Cancel otherwise.</remarks>
	/// <remarks>If Result is NotInList, delete the anime.</remarks>
	public class AnimeDialog : Dialog {
		// TODO process triggers in AnimeWindow
		// TODO create EntryDialog and make this implement that (readability, save time)
		/// <summary>
		/// A copy of the given media with updated changes.
		/// </summary>
		public readonly Anime Result;
		
		private readonly Anime _original;
		private bool _changed; // was anything modified?
		private bool _create; // should we create a new entry?
		
		private Notebook _nb;
		//private VBox _infoBox, _editBox, _episodeBox, _characterBox;
		private ScrolledWindow _listWindow;
		private SpinButton _episodeSpin, _scoreSpin;
		private ComboBox _statusBox;
		private DatePicker _userStart, _userEnd;
		private TextView _notesEntry; 
		private Button _okButton, _cancelButton, _addRemoveButton;
		
		/// <summary>
		/// Dialog for showing anime data and editing list information
		/// </summary>
		/// <param name="a">The anime to use</param>
		/// <remarks>Resonds with Accept if data was changed and should be replaced.</remarks>
		public AnimeDialog(Anime a) {
			_original = a;
			Result = new Anime(a);

			Title = Program.GetTitle(a);
			TransientFor = Program.Win;
			DestroyWithParent = true;
			WindowPosition = WindowPosition.CenterOnParent;
			Role = "Media";
			TypeHint = WindowTypeHint.Dialog;
			DefaultSize = new Size(500, 450);
			
			Instantiate();
			Build();

			DeleteEvent += delegate { Respond(ResponseType.Cancel); };
			_cancelButton.Clicked += delegate { Respond(ResponseType.Cancel); };
			_okButton.Clicked += delegate { Respond(_changed ? ResponseType.Accept : ResponseType.Cancel); };
			ShowAll();
		}

		private void Instantiate() {
			_nb = new Notebook();
			
			var max = (_original.Episodes == 0 ? double.MaxValue : _original.Episodes);
			var adj = new Adjustment(_original.CurrentEpisode, 0, max, 1, 10, 0);
			_episodeSpin = new SpinButton(adj, 1, 0);
			
			_scoreSpin = new SpinButton(new Adjustment(_original.UserScore, 0, 10, 1, 2, 0), 1, 0);
			_notesEntry = new TextView {Buffer = {Text = _original.Notes}};
			_statusBox = new ComboBox(new [] { "Currently Watching", "Completed", "On Hold", "Dropped", "Planned" }); // lines up with ListStatus enum
			_statusBox.Active = (int)_original.ListStatus;
			_userStart = new DatePicker(_original.UserStart);
			_userEnd = new DatePicker(_original.UserEnd);
			_okButton = new Button("OK");
			_cancelButton = new Button("Cancel");

			if(_original.ListStatus == ApiEntry.ListStatuses.NotInList) {
				_addRemoveButton = new Button(Stock.Add);
				_addRemoveButton.Clicked += OnAdd;
			}
			else {
				_addRemoveButton = new Button(Stock.Remove);
				_addRemoveButton.Clicked += OnDelete;
			}
		}

		private void Build() {


			// build the header
			var header = new HBox(false, 5); // contains the top
			var imagebox = new VBox(); // contains the picture
			try {
				// get the image
				var stream = _original.GetCover().Result;
				var cover = new Pixbuf(stream, 113, 159);
				imagebox.PackStart(new Image(cover), false, false, 0);
			}
			catch(Exception e) {
				Debug.WriteLine("Render failed! " + e.Message);
			}
			finally {
				header.PackStart(imagebox, false, false, 0);
			}
			var titlebox = new VBox(); // contains the title 
			var titlefont = new FontDescription() {
				Weight = Weight.Bold,
				Size = (int)(14 * Pango.Scale.PangoScale),

			};
			var title = new Label(Program.GetTitle(_original));
			title.ModifyFont(titlefont);
			titlebox.PackStart(title, true, false, 0);
			var spinbox = new HBox(false, 0); // contains the episodes
			spinbox.PackStart(new Label("Episode") {Justify = Justification.Right}, false, false, 20);
			spinbox.Add(_episodeSpin);
			_episodeSpin.SetSizeRequest(50, 22);
			_episodeSpin.ValueChanged += OnEpisodeChanged;
			spinbox.Add(new Label($"/ {_original.Episodes}"));
			spinbox.PackEnd(new VBox(), true, true, 100); // push everything left
			titlebox.Add(spinbox);
			var labels = new[] {
				// all the header title info goes here
				$"Type: \t\t{Enum.GetName(typeof(Anime.ShowTypes), _original.Type)}",
				$"Season: \t{_original.Season}",
				$"Status: \t{Enum.GetName(typeof(Anime.RunningStatuses), _original.Status)}",
				$"Score:\t\t{_original.Score}"
			};
			foreach(var s in labels) {
				var hbox = new HBox(false, 10);
				hbox.PackStart(new Label(s), false, false, 20);
				hbox.PackEnd(new VBox(), true, true, 100);
				titlebox.Add(hbox);
			}
			header.Add(titlebox);
			VBox.PackStart(header, false, false, 5);

			// List Values
			var table = new Table(4, 9, false);
			table.Attach(new Label("List Status"), 0, 1, 0, 1);
			table.Attach(_statusBox, 2, 3, 0, 1);
			_statusBox.Changed += OnStatusChanged;
			table.Attach(new Label("Score"), 0, 1, 1, 2);
			table.Attach(_scoreSpin, 2, 3, 1, 2);
			_scoreSpin.Changed += OnScoreChnaged;
			table.Attach(new Label("Started At"), 0, 1, 2, 3);
			table.Attach(_userStart, 2, 4, 2, 3);
			_userStart.Changed += OnStartDateChanged;
			table.Attach(new Label("Completed At"), 0, 1, 3, 4);
			table.Attach(_userEnd, 2, 4, 3, 4);
			_userEnd.Changed += OnEndDateChanged;
			table.Attach(new Label("Notes"), 0, 1, 4, 5);
			table.Attach(new ScrolledWindow() { new Viewport() {_notesEntry}}, 1, 3, 4, 6);
			_notesEntry.Buffer.Changed += OnNotesChanged;
			
			var viewport = new Viewport { new HBox(false, 5) {table }};
			_listWindow = new ScrolledWindow() {viewport};
			if(_original.ListStatus == ApiEntry.ListStatuses.NotInList)
				_nb.AppendPage(_listWindow, new Label("List"));
			
			
			// Notebook
			VBox.Add(_nb);
			
			// buttons
			var buttonbox = new HBox(false, 5);
			buttonbox.PackStart(_addRemoveButton, false, false, 0); // make the delete button 30x30
			buttonbox.Add(_okButton);
			buttonbox.Add(_cancelButton);
			ActionArea.Add(buttonbox);
			_okButton.GrabDefault();
			_okButton.Clicked += OnOk;
			_cancelButton.Clicked += OnCancel;
			_addRemoveButton.SetSizeRequest(30, 30);
			_okButton.SetSizeRequest(70, 30);
			_cancelButton.SetSizeRequest(70, 30);
			_addRemoveButton.Visible = _original.ListStatus != ApiEntry.ListStatuses.NotInList; // don't show the button if we cant delete
		}

		private void OnEpisodeChanged(object o, EventArgs args) {
			var spin = (SpinButton) o;
			Result.CurrentEpisode = spin.ValueAsInt;
			_changed = true;
		}

		private void OnStatusChanged(object o, EventArgs args) {
			// 0 is NotInList!
			Result.ListStatus = (ApiEntry.ListStatuses)(_statusBox.Active + 1);
			_changed = true;
		}

		private void OnScoreChnaged(object o, EventArgs args) {
			Result.UserScore = _scoreSpin.ValueAsInt;
			_changed = true;
		}
		
		private void OnStartDateChanged(object o, EventArgs args) { // attach these
			Result.UserStart = _userStart.Value;
			_changed = true;
		}


		private void OnEndDateChanged(object o, EventArgs args) {
			Result.UserEnd = _userEnd.Value;
		}

		private void OnNotesChanged(object o, EventArgs args) {
			Result.Notes = _notesEntry.Buffer.Text;
		}

		private void OnOk(object o, EventArgs args) {
			if(_create)
				Respond(ResponseType.Accept);
			else if(_changed)
				Respond(ResponseType.Apply);
			else Respond(ResponseType.Cancel);
		}

		private void OnCancel(object o, EventArgs args) {
			Respond(ResponseType.Cancel);
		}

		private void OnAdd(object o, EventArgs args) {
			_create = true;
			_nb.AppendPage(_listWindow, new Label("List"));
			_addRemoveButton.Visible = false;
		}

		private void OnDelete(object o, EventArgs args) {
			var d = new MessageDialog(Program.Win, DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.YesNo, "Delete the selected anime?");
			if(d.Run() == (int)ResponseType.Yes) {
				Result.ListStatus = ApiEntry.ListStatuses.NotInList;
				Respond(ResponseType.Apply);
			}
			d.Destroy();
		}
	}
}