using System;
using System.Security.Cryptography;
using Gtk;
using Window = Gtk.Window;

namespace Trackr.Gui.Gtk {
	public class SettingsWindow : Window {

		private readonly bool _forced;

		private VBox _container;
		private Notebook _nb;
		private Frame _general, _accounts, _recognition, _torrents;
		private HBox _buttons;
		private Alignment _hAlign;

		// General
		private CheckButton _onTop; 
		
		// Accounts
		private VBox _acctBox1;
		private enum Columns { Used, Username, Service}
		private ListStore _acctStore;
		private TreeView _acctTree;
		private ScrolledWindow _acctSw;
		private Label _acctLabel;
		private Button _acctAdd, _acctRem;
		private HBox _acctB;
		private Alignment _acctbAlign;
			
		private Button _ok, _cancel;
		
		/// <summary>
		/// Spawn a new Settings dialog box
		/// </summary>
		/// <param name="forced">Set to true if there was no settings definiton beforehand.</param>
		public SettingsWindow(bool forced) : base("Settings") {
			_forced = forced;
			DefaultSize = new Gdk.Size(500, 450);
			DestroyWithParent = true;
			Role = "settings";
			WindowPosition = _forced ? WindowPosition.Center : WindowPosition.CenterOnParent;

			Instantiate();
			Build();
			
			// Set the Event Handlers
			DeleteEvent += OnDelete;
			ShowAll();

			FillWindow();
			if(_forced) _nb.CurrentPage = 1; // Open to Accounts tab
		}

		
		
		private void Instantiate() {
			// Lets make our containers
			_container = new VBox(false, 3);
			_nb = new Notebook();
			_general = new Frame();
			_accounts = new Frame();
			_recognition = new Frame();
			_torrents = new Frame();
			_buttons = new HBox(true, 3);
			_hAlign = new Alignment(1, 1, 0, 0);

			// The buttons that go on the bottom
			_ok = new Button("OK");
			_cancel = new Button("Cancel");

			// General Tab
			_onTop = new CheckButton("Keep Window on Top");

			// Accounts Tab
			_acctBox1 = new VBox(false, 3);
			_acctStore = new ListStore(typeof(string), typeof(string), typeof(string));
			_acctTree = new TreeView(_acctStore);
			_acctSw = new ScrolledWindow();
			_acctLabel = new Label("Choose the accounts to use below. Double click on an account to change account settings.");
			_acctB = new HBox(true, 3);
			_acctbAlign = new Alignment(1, 1, 0, 0);
			_acctAdd = new Button(new Image(Stock.Add, IconSize.Button));
			_acctRem = new Button(new Image(Stock.Remove, IconSize.Button));
		}


		
		private void Build() {
			// Set up our notebook
			Add(_container);
			_container.PackStart(_nb, true, true, 0);
			_nb.InsertPage(_general, new Label("General"), -1);
			_nb.InsertPage(_accounts, new Label("Accounts"), -1);
			_nb.InsertPage(_recognition, new Label("Recognition"), -1);
			_nb.InsertPage(_torrents, new Label("Torrents"), -1);
			
			// Lets add our buttons that go on the bottom
			_container.PackEnd(_hAlign, false, false, 0);
			_hAlign.SetPadding(0, 5, 0, 5);
			_hAlign.Add(_buttons);
			_buttons.Add(_ok);
			_ok.SetSizeRequest(70, 30);
			_buttons.Add(_cancel);
			_cancel.Clicked += delegate { Destroy(); };
			
			// General tab
			_general.Add(_onTop);
			
			// Accounts tab
			_acctSw.ShadowType = ShadowType.EtchedIn;
			_acctSw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			_acctBox1.BorderWidth = 10;
			//Add the tree columns
			var i = 0;
			foreach(var x in Enum.GetNames(typeof(Columns))) {
				var crt = new CellRendererText();
				var c = new TreeViewColumn(x, crt, "text", i) {SortColumnId = i};
				_acctTree.AppendColumn(c);
				i++;
			}
			_accounts.Add(_acctBox1);
			_acctBox1.PackStart(_acctLabel, false, false, 10);
			_acctBox1.Add(_acctSw);
			_acctSw.Add(_acctTree);
			_acctB.Add(_acctAdd);
			_acctB.Add(_acctRem);
			_acctbAlign.Add(_acctB);
			_acctBox1.PackEnd(_acctbAlign, false, false, 0);
		}

		private void FillWindow() {
			// General
			if (Program.Settings.KeepWindowOnTop) _onTop.Active = true;
			
			// Accounts
			var defAnime = Program.Settings.DefaultAnime.Split('@');
			var defManga = Program.Settings.DefaultManga.Split('@');
			foreach (var act in Program.Settings.Accounts) {
				var defAccount = string.Empty;
				if (defAnime[0] == act.Key && defAnime[1] == act.Value.Username)
					defAccount += "A";
				if (defManga[0] == act.Key && defManga[1] == act.Value.Password)
					defAccount += "M";
				AddToAccountList(defAccount, act.Value.Username, act.Key);
			}
		}
		
		private void AddToAccountList(string defAccount, string username, string api) {
			_acctStore.AppendValues(defAccount, username, api);
		}

		private void OnDelete(object o, DeleteEventArgs args) {
			if(_forced)
				Application.Quit();
		}
	}
}