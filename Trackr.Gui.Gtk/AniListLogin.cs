using System;
using System.Text.RegularExpressions;
using Gtk;
using Trackr.Api;


namespace Trackr.Gui.Gtk {
	/// <summary>
	/// A dialog for retrieving the AniList OAuth pin from the user
	/// </summary>
	public class AniListLogin : Dialog {
		/// <summary>
		/// The retreived pin
		/// </summary>
		public string Pin;
		private  Entry _pinEntry;
		private Button _ok, _cancel;
		
		
		public AniListLogin() {
			Title = "Log In To AniList";
			KeepAbove = true;
			WindowPosition = WindowPosition.Center;
			Build();
			ShowAll();
			System.Diagnostics.Process.Start(AniList.RedirectUrl); // Open the page to log in
		}

		private void Build() {
			BorderWidth = 10;
			_pinEntry = new Entry();
			_ok = new Button("OK");
			_cancel = new Button("Cancel");
			
			VBox.PackStart(
				new Label("To continue, please log into AniList and paste the authorization pin here.") {
					Justify = Justification.Center
				}, false, false, 10);
			VBox.Add(_pinEntry);
			
			// Buttons
			_ok.SetSizeRequest(70, 30);
			ActionArea.Homogeneous = true;
			ActionArea.PackEnd(_ok, false, false, 5);
			ActionArea.PackEnd(_cancel, false, false, 0);
			
			//Events
			DestroyEvent += OnCancel;
			_ok.Clicked += OnOk;
			_cancel.Clicked += OnCancel;
		}

		private void OnOk(object o, EventArgs e) {
			_pinEntry.Text = Pin;
			Respond(ResponseType.Accept);
		}

		private void OnCancel(object o, EventArgs e) {
			Respond(ResponseType.Reject);
		}
	}
}