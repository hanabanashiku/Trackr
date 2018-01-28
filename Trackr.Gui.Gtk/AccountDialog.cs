using System;
using Gtk;
using Trackr.Api;
using Trackr.Core;

namespace Trackr.Gui.Gtk {
	/// <summary>
	/// A dialog box used to add an account to Trackr's settings
	/// </summary>
	internal class AccountDialog : Dialog {
		// TODO: Handle editing accounts (maybe with another constructor or seperate class?
		// TODO: Allow for making accounts the default account
		/// <summary>
		/// The resulting account
		/// </summary>
		public Account Result;
		private ComboBox _type;
		private Entry _username, _password;
		private Button _okButton, _cancelButton;

		public AccountDialog() {
			Title = "Add Account";
			BorderWidth = 10;
			WindowPosition = WindowPosition.Center;
			TypeHint = Gdk.WindowTypeHint.Dialog;
			Build();
		}

		private void Build() {
			string[] options = {"MyAnimeList", "Kitsu", "AniList"};
			_type = new ComboBox(options);
			_type.Changed += OnTextChange;
			_username = new Entry();
			_username.Changed += OnTextChange;
			_password = new Entry() { Visibility =  false };
			_password.Changed += OnTextChange;
			_okButton = new Button("OK");
			_okButton.SetSizeRequest(70, 30);
			_okButton.Clicked += OnOkButton;
			_okButton.Sensitive = false;
			_cancelButton = new Button("Cancel");
			_cancelButton.SetSizeRequest(70, 30);
			_cancelButton.Clicked += delegate { Respond(ResponseType.Cancel); };

			var hb1 = new HBox();
			hb1.PackStart(new Label("Username"), false, false, 7);
			hb1.Add(_username);
			VBox.Add(hb1);

			var hb2 = new HBox();
			hb2.PackStart(new Label("Password"), false, false, 7);
			hb2.Add(_password);
			VBox.Add(hb2);

			var hb3 = new HBox();
			hb3.PackStart(new Label("Type"), false, false, 7);
			hb3.Add(_type);
			VBox.Add(hb3);
				
			var align = new Alignment(1, 1, 0, 0);
			ActionArea.Add(_okButton);
			ActionArea.Add(_cancelButton);
			ShowAll();
		}

		// Don't submit if it isn't filled out!
		private void OnTextChange(object o, EventArgs args) {
			if(_username.Text.Length == 0 || _password.Text.Length == 0 || _type.Active == -1)
				_okButton.Sensitive = false;
			else _okButton.Sensitive = true;
		}
		
		private async void OnOkButton(object o, EventArgs args) {
			_okButton.Sensitive = false;
			switch(_type.ActiveText) {
				
				case "MyAnimeList":
					var cred = new UserPass(_username.Text, _password.Text);
					var api = new MyAnimeList(cred);
					bool res;
					try {
						res = api.VerifyCredentials().Result;
					}
					// ApiRequestException, WebException...
					catch(Exception) {
						var ed = new MessageDialog(null, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.OkCancel, "The request has timed out.");
						var ret = ed.Run();
						ed.Destroy();
						if(ret == (int)ResponseType.Cancel)
							Respond(ResponseType.Reject);
						_okButton.Sensitive = true;
						return;
					}
					
					if(res) {
						Result = new Account(api.Name, api.Username, cred);
						Respond(ResponseType.Accept);
					}
					else { // Invalid username or password!
						var ed = new MessageDialog(null, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.OkCancel,
							"Invalid username or password.") {WindowPosition = WindowPosition.Center};
						var ret = ed.Run();
						ed.Destroy();
						if(ret == (int)ResponseType.Cancel)
							Respond(ResponseType.Reject);
					}
					_okButton.Sensitive = true;
					break;
					
				default:
					var md = new MessageDialog(null, DialogFlags.DestroyWithParent, MessageType.Warning, ButtonsType.Ok,
						"The selected account type has not yet been implemented.");
					md.WindowPosition = WindowPosition.Center;
					md.Run();
					md.Destroy();
					_okButton.Sensitive = true;
					break;
			}
		}
	}
}