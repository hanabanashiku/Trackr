using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Gdk;
using Gtk;
using Pango;
using Trackr.Api;
using Image = Gtk.Image;
using Alignment = Gtk.Alignment;

namespace Trackr.Gui.Gtk {
	/// <summary>
	/// 
	/// </summary>
	public class AnimeDialog : Dialog {
		
		/// <summary>
		/// A copy of the given media with updated changes.
		/// </summary>
		public Anime Result;
		
		private Anime _original;
		private bool _changed;

		private Notebook _nb;
		private VBox _infoBox, _editBox, _episodeBox, _characterBox;
		private SpinButton _episodeSpin;
		private Button _okButton, _cancelButton, _deleteButton;
		
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
			
			_deleteButton = new Button(new Image(Stock.Delete, IconSize.Button));
			_okButton = new Button("OK");
			_cancelButton = new Button("Cancel");
		}

		private void Build() {
			VBox.BorderWidth = 20;
			
			// build the header
			var header = new HBox(false, 0); // contains the top
			var imagebox = new VBox(); // contains the picture
			try { // get the image
				var stream = _original.GetCover().Result;
				var cover = new Pixbuf(stream, 134, 190);
				imagebox.PackStart(new Image(cover), false, false, 0);
			}
			catch(Exception e) {
				Debug.WriteLine("Render failed! " + e.Message);
			}
			finally {
				header.PackStart(imagebox, false, false, 0);
			}
			var titlebox = new VBox(); // contains the title 
			var titlefont = new Pango.FontDescription() {
				Weight = Weight.Bold,
				Size = (int)(14 * Pango.Scale.PangoScale),
				
			}; //TODO fix alignment issue
			var title = new Label(Program.GetTitle(_original));
			title.ModifyFont(titlefont);
			titlebox.PackStart(title, true, false, 0);
			var spinbox = new HBox(false, 0); // contains the episodes
			spinbox.PackStart(new Label("Episode") { Justify = Justification.Right}, false, false, 20);
			spinbox.Add(_episodeSpin);
			_episodeSpin.SetSizeRequest(50, 22);
			spinbox.Add(new Label($"/ {_original.Episodes}"));
			spinbox.PackEnd(new VBox(), true, true, 100); // push everything left
			titlebox.Add(spinbox);
			var databox = new VBox(true, 1); // contains basic series data
			databox.PackStart( new Label($"Type: \t{Enum.GetName(typeof(Anime.ShowTypes), _original.Type)}"), true, true, 5);
			databox.Add(new Label($"Status: \t{Enum.GetName(typeof(Anime.RunningStatuses), _original.Status)}"));
			databox.Add(new Label($"Score: \t{_original.Score}"));
			titlebox.PackStart(new Alignment(0, 1, 0, 0) {databox}, true, true, 10);
			header.Add(titlebox);
			VBox.PackStart(header, false, false, 5);
			VBox.Add(new HBox() {_nb});
			
			// buttons
			var buttonbox = new HBox(false, 5);
			buttonbox.PackStart(_deleteButton, false, false, 0); // make the delete button 30x30
			buttonbox.Add(_okButton);
			buttonbox.Add(_cancelButton);
			ActionArea.Add(buttonbox);
			_okButton.GrabDefault();
			_deleteButton.SetSizeRequest(30, 30);
			_okButton.SetSizeRequest(70, 30);
			_cancelButton.SetSizeRequest(70, 30);
			_deleteButton.Visible = _original.ListStatus != ApiEntry.ListStatuses.NotInList; // don't show the button if we cant delete
		}
	}
}