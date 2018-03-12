using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Gdk;
using Gtk;

using Trackr.Api;
using Image = Gtk.Image;

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
			_deleteButton = new Button(new Image(Stock.Delete, IconSize.Button));
			_okButton = new Button("OK");
			_cancelButton = new Button("Cancel");
		}

		private void Build() {
			
			var hbox = new HBox(false, 5);
			hbox.PackStart(_deleteButton, false, false, 0);
			hbox.Add(_okButton);
			hbox.Add(_cancelButton);
			ActionArea.Add(hbox);
			_okButton.GrabDefault();
			_deleteButton.SetSizeRequest(30, 30);
			_okButton.SetSizeRequest(70, 30);
			_cancelButton.SetSizeRequest(70, 30);
		}
	}
}