using System;
using System.Linq;
using Gdk;
using Gtk;

namespace Trackr.Gui.Gtk {
	public class SettingsWindow : Dialog {		
		// If the user has never seen this window before, we want to make sure the 
		private readonly bool _forced; 

		private Notebook _nb;
		private Frame _general, _recognition, _torrents;
		private AccountsManager _accounts;
		private HBox _buttons;
		private Alignment _hAlign;

		// General
		private CheckButton _onTop; 
			
		private Button _ok, _cancel, _apply;
		
		/// <summary>
		/// Spawn a new Settings dialog box
		/// </summary>
		/// <remarks>This dialog box will change the settings values on its own if they are applied.</remarks>
		/// <param name="forced">Set to true if there was no settings definiton beforehand.</param>
		public SettingsWindow(bool forced){
			_forced = forced;
			Title = "Settings";
			DefaultSize = new Size(500, 450);
			if(Program.Win != null && Program.Win.Visible) {
				TransientFor = Program.Win;
				DestroyWithParent = true;
				WindowPosition = WindowPosition.CenterOnParent;
			}
			else WindowPosition = WindowPosition.Center;
			Icon = IconTheme.Default.LoadIcon(Stock.Preferences, 16, IconLookupFlags.UseBuiltin);
			Role = "settings";

			Instantiate();
			Build();
			
			// Set the Event Handlers
			DeleteEvent += OnDelete;
			ShowAll();

			Fill();
			if(_forced) _nb.CurrentPage = 1; // Open to Accounts tab
		}

		
		
		private void Instantiate() {
			// Lets make our containers
			_nb = new Notebook();
			_general = new Frame();
			_accounts = new AccountsManager();
			_recognition = new Frame();
			_torrents = new Frame();
			_buttons = new HBox(true, 3);
			_hAlign = new Alignment(1, 1, 0, 0);

			// The buttons that go on the bottom
			_ok = new Button("OK") {CanDefault = true};
			_cancel = new Button("Cancel");
			_apply = new Button("Apply");

			// General Tab
			_onTop = new CheckButton("Keep Window on Top");
		}


		
		private void Build() {
			// Set up our notebook
			VBox.Homogeneous = false;
			VBox.Spacing = 3;
			VBox.PackStart(_nb, true, true, 0);
			_nb.InsertPage(_general, new Label("General"), -1);
			_nb.InsertPage(_accounts, new Label("Accounts"), -1);
			_nb.InsertPage(_recognition, new Label("Recognition"), -1);
			_nb.InsertPage(_torrents, new Label("Torrents"), -1);
			
			// Lets add our buttons that go on the bottom
			ActionArea.PackEnd(_hAlign, false, false, 0);
			_hAlign.SetPadding(0, 5, 0, 5);
			_hAlign.Add(_buttons);
			_buttons.Add(_ok);
			_ok.Clicked += delegate {
				Apply();
				Respond(ResponseType.Accept);
			};
			_ok.GrabDefault();
			_ok.SetSizeRequest(70, 30);
			_buttons.Add(_cancel);
			_cancel.Clicked += delegate { 
				Respond(ResponseType.Cancel);
			};
			_buttons.Add(_apply);
			_apply.Clicked += delegate { Apply(); };
			
			// General tab
			_general.Add(_onTop);
		}

		// Fill the window with information currently in the settings file
		private void Fill() {
			// General
			if (Program.Settings.KeepWindowOnTop) _onTop.Active = true;
			
			// Accounts
			_accounts.Fill();
		}

		// Apply all the settings that have been modified
		private void Apply() {
			// General
			Program.Settings.KeepWindowOnTop = _onTop.Active;

			// Sync accounts
			var accounts = Program.Settings.Accounts;
			// Remove all accounts that don't have an account in our copy with the same Username and Provider.
			accounts.RemoveAll(x => !_accounts.AccountList.Contains(x));
			// Add all accounts that are not yet contained in our settings list.
			_accounts.AccountList.Where(x => !accounts.Contains(x)).ToList().ForEach(x => accounts.Add(x));
			foreach(var a in accounts) {
				var b = _accounts.AccountList.First(x => x == a);
				if(a.Credentials.Password != b.Credentials.Password)
					a.Credentials.Password = b.Credentials.Password;
			}
			// Set the default accounts
			if(Program.Settings.DefaultAnime != _accounts.DefAnime)
				Program.Settings.DefaultAnime = _accounts.DefAnime == null ? null : accounts.First(x => x == _accounts.DefAnime);
			if(Program.Settings.DefaultManga != _accounts.DefManga)
				Program.Settings.DefaultManga = _accounts.DefManga == null ? null : accounts.First(x => x == _accounts.DefManga);
		}

		// The user clicked the close button
		private void OnDelete(object o, DeleteEventArgs args) {
			if(_forced) { 
				Application.Quit();
				Environment.Exit(0);
			}
			else Respond(ResponseType.Cancel);
		}

	}
}