using System;
using Gdk;
using Gtk;

namespace Trackr.Gui.Gtk {
	public class SettingsWindow : Dialog {
//TODO: Add Apply button
		
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
		/// <param name="forced">Set to true if there was no settings definiton beforehand.</param>
		public SettingsWindow(bool forced){
			_forced = forced;
			Title = "Settings";
			DefaultSize = new Size(500, 450);
			DestroyWithParent = true;
			Icon = IconTheme.Default.LoadIcon(Stock.Preferences, 64, IconLookupFlags.UseBuiltin);
			Role = "settings";
			WindowPosition = _forced ? WindowPosition.Center : WindowPosition.CenterOnParent;

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
			_ok = new Button("OK");
			_ok.CanDefault = true;
			_cancel = new Button("Cancel");

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
			_ok.GrabDefault();
			_ok.SetSizeRequest(70, 30);
			_buttons.Add(_cancel);
			_cancel.Clicked += delegate { Respond(ResponseType.Cancel); };
			
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
			
			//TODO
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