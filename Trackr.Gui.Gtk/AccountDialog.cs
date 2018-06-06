using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Gtk;
using Trackr.Api;
using Trackr.Core;

namespace Trackr.Gui.Gtk {
	/// <summary>
	/// A dialog box used to add an account to Trackr's settings or edit an existing one
	/// </summary>
	/// <remarks>This dialog will respond with Accept if the account can be added,
	/// Reject if it cannot, or Cancel if the user cancelled the operation.</remarks>
	internal class AccountDialog : Dialog {
		/// <summary>
		/// The resulting account
		/// </summary>
		public Account Result;
		/// <summary>
		/// This account should be the default anime account.
		/// </summary>
		public bool DefaultAnime;
		/// <summary>
		/// This account should be the default manga account.
		/// </summary>
		public bool DefaultManga;
		
		private ComboBox _type;
		private Label _userLabel, _passLabel;
		private Entry _username, _password;
		private Button _okButton, _cancelButton;
		private CheckButton _defAnimeCheck, _defMangaCheck;
		private readonly string[] _options = {"MyAnimeList", "Kitsu", "AniList"};

		/// <summary>
		/// A constructor for account adding.
		/// </summary>
		public AccountDialog() {
			Title = "Add Account";
			BorderWidth = 10;
			WindowPosition = WindowPosition.Center;
			TypeHint = Gdk.WindowTypeHint.Dialog;
			Build();
		}

		/// <summary>
		/// A constructor for account editing.
		/// </summary>
		public AccountDialog(string username, UserPass cred, string provider, string def) : this() {
			Title = "Edit Account";
			_username.Text = username;
			_username.Sensitive = false;
			_password.Text = cred.Password; // Does not allow you to copy password out
			_type.Active = Array.FindIndex(_options, x => x.Equals(provider));
			_type.Sensitive = false;
			if(def.Contains("A"))
				_defAnimeCheck.Active = true;
			if(def.Contains("M"))
				_defMangaCheck.Active = true;
		}

		private void Build() {
			_type = new ComboBox(_options);
			_type.Changed += OnProviderChange;
			_userLabel = new Label("Username");
			_username = new Entry();
			_username.Changed += OnTextChange;
			_passLabel = new Label("Password");
			_password = new Entry() { Visibility =  false };
			_password.Changed += OnTextChange;
			_defAnimeCheck = new CheckButton("Use this account for managing anime") { Name = "defAnime", Sensitive = false };
			_defAnimeCheck.Toggled += OnToggle;
			_defMangaCheck = new CheckButton("Use this account for managing manga") { Name = "defManga", Sensitive = false };
			_defMangaCheck.Toggled += OnToggle;
			_okButton = new Button("OK");
			_okButton.SetSizeRequest(70, 30);
			_okButton.CanDefault = true;
			_okButton.Clicked += OnOkButton;
			_okButton.Sensitive = false;
			_cancelButton = new Button("Cancel");
			_cancelButton.SetSizeRequest(70, 30);
			_cancelButton.Clicked += delegate { Respond(ResponseType.Cancel); };

			var hb1 = new HBox();
			hb1.PackStart(_userLabel, false, false, 7);
			hb1.Add(_username);
			VBox.Add(hb1);

			var hb2 = new HBox();
			hb2.PackStart(_passLabel, false, true, 7);
			hb2.Add(_password);
			VBox.Add(hb2);

			var bb = new VButtonBox {_defAnimeCheck, _defMangaCheck};
			VBox.Add(bb);
			
			var hb3 = new HBox();
			hb3.PackStart(new Label("Type"), false, false, 7);
			hb3.Add(_type);
			VBox.Add(hb3);
				
			ActionArea.Add(_okButton);
			_okButton.GrabDefault(); // Activates when you hit enter
			ActionArea.Add(_cancelButton);
			ShowAll();
		}

		// Don't submit if it isn't filled out!
		private void OnTextChange(object o, EventArgs args) {
			if((_type.ActiveText != "AniList") &&
			   (_username.Text.Length == 0 || _password.Text.Length == 0 || _type.Active == -1))
				_okButton.Sensitive = false;
			else _okButton.Sensitive = true;
		}

		private void OnProviderChange(object o, EventArgs args) {
			switch(_type.ActiveText) {
					case "MyAnimeList": case "Kitsu":
						_defAnimeCheck.Sensitive = true;
						_defMangaCheck.Sensitive = true;
						_userLabel.Visible = true;
						_username.Visible = true;
						_passLabel.Visible = true;
						_password.Visible = true;
						break;
					case "AniList": // AniList requires a pin!
						_defAnimeCheck.Sensitive = true;
						_defMangaCheck.Sensitive = true;
						_userLabel.Visible = false;
						_username.Visible = false;
						_passLabel.Visible = false;
						_password.Visible = false;
						break;
				default:
					_defAnimeCheck.Active = false;
					_defAnimeCheck.Sensitive = false;
					_defMangaCheck.Active = false;
					_defMangaCheck.Sensitive = false;
					break;
			}
			OnTextChange(o, args);
		}

		// Check if this account should be a default account
		private void OnToggle(object o, EventArgs e) {
			var box = (CheckButton) o;
			switch(box.Name) {
				case "defAnime":
					DefaultAnime = _defAnimeCheck.Active;
					break;
				case "defManga":
					DefaultManga = _defMangaCheck.Active;
					break;
				default:
					Debug.Fail("Invalid default checkbox name!");
					break;
			}
		}
		
		private async void OnOkButton(object o, EventArgs args) {
			UserPass cred;
			Api.Api api;
			var res = false;
			
			_okButton.Sensitive = false;
			switch(_type.ActiveText) {
				
				case "MyAnimeList": case "Kitsu":
					cred = new UserPass(_username.Text, _password.Text);
					if(_type.ActiveText == "MyAnimeList")
						api = new MyAnimeList(cred);
					else //if(_type.ActiveText == "Kitsu")
						api = new Kitsu(cred);
					try {
						res = Task.Run(() => api.VerifyCredentials()).Result;
					}
					// ApiRequestException, WebException...
					catch(Exception e) {
						var ed = new MessageDialog(null, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.OkCancel,
							"The request has timed out.") {WindowPosition = WindowPosition.Center};
						var ret = ed.Run();
						ed.Destroy();
						Debug.WriteLine("[Exception] " + (e.InnerException?.Message ?? e.Message));
						Debug.WriteLineIf(e.InnerException != null, e.InnerException?.StackTrace);
						
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
						else _okButton.Sensitive = true;
					}
					break;
				
				case "AniList":
					var ad = new AniListLogin();
					if(ad.Run() == (int)ResponseType.Accept) {
						cred = new UserPass(null, ad.Pin);
						api = new AniList(cred);
						try {
							res = await api.VerifyCredentials();
						}
						catch(Exception e) {
							var ed = new MessageDialog(null, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.Ok,
								e.InnerException?.Message ?? e.Message) {WindowPosition = WindowPosition.Center};
							ed.Run();
							ed.Destroy();
							Respond(ResponseType.Reject);
							Debug.WriteLine("[Exception] " + (e.InnerException?.Message ?? e.Message));
							ad.Destroy();
							return;
						}

						if(res) {
							Result = new Account(api.Name, api.Username, cred);
							Respond(ResponseType.Accept);
						}
						else { // Likely an invalid pin!
							var ed = new MessageDialog(null, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.Ok,
								"An invalid authentication pin was entered.") {WindowPosition = WindowPosition.Center};
							ed.Run();
							ed.Destroy();
							ad.Destroy();
							Respond(ResponseType.Reject);
						}
					}
					else
						Respond(ResponseType.Reject);
					ad.Destroy();
					break;
				default:
					Debug.Fail($"The account type {_type.ActiveText} has not been implemented.");
					break;
			}
		}
	}
}