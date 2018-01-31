using System;
using System.Collections.Generic;
using System.Linq;
using Gdk;
using Gtk;
using Trackr.Core;
using Image = Gtk.Image;
using Window = Gtk.Window;

namespace Trackr.Gui.Gtk {
	public class SettingsWindow : Window {
//TODO: Convert this to a dialog box :(
//TODO: Add Apply button
//TODO: Remove button
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
		private Button _acctAdd, _acctEdit, _acctRem;
		private HBox _acctB;
		private Alignment _acctbAlign;
			
		private Button _ok, _cancel;

		private List<Account> _accountList;
		private Account _defAnime;
		private Account _defManga;
		
		/// <summary>
		/// Spawn a new Settings dialog box
		/// </summary>
		/// <param name="forced">Set to true if there was no settings definiton beforehand.</param>
		public SettingsWindow(bool forced) : base("Settings") {
			_forced = forced;
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
			_acctEdit = new  Button(new Image(Stock.Edit, IconSize.Button));
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
				_acctTree.AppendColumn(x, new CellRendererText(), "text", i);
				i++;
			}
			_accounts.Add(_acctBox1);
			_acctBox1.PackStart(_acctLabel, false, false, 10);
			_acctBox1.Add(_acctSw);
			_acctSw.Add(_acctTree);
			_acctTree.RowActivated += OnRowActivated;
			_acctB.Add(_acctAdd);
			_acctAdd.Clicked += OnAddAccount;
			_acctB.Add(_acctEdit);
			_acctEdit.Clicked += OnEditClick;
			_acctB.Add(_acctRem);
			_acctbAlign.Add(_acctB);
			_acctBox1.PackEnd(_acctbAlign, false, false, 0);
		}

		private void FillWindow() {
			// General
			if (Program.Settings.KeepWindowOnTop) _onTop.Active = true;
			
			
			// Accounts
			// Here we are going to create a copy of the account list and update if necessary
			_accountList = new List<Account>(Program.Settings.Accounts.Count);
			Program.Settings.Accounts.ForEach(x => _accountList.Add(new Account(x)));
			_defAnime = Program.Settings.DefaultAnime == null ? null : new Account(Program.Settings.DefaultAnime);
			_defManga = Program.Settings.DefaultManga == null ? null : new Account(Program.Settings.DefaultManga);
			
			foreach (var act in _accountList)
				AddToStore(act);
		}

		// get a string representing the default accounts
		private string GetDefaultAccounts(Account a) {
			var defAccount = string.Empty;
			if (_defAnime == a)
				defAccount += "A";
			if (_defManga == a)
				defAccount += "M";	
			return defAccount;
		}
		
		// Add the account to the table
		private void AddToStore(Account a) {
			var defAccount = GetDefaultAccounts(a);
			 _acctStore.AppendValues(defAccount, a.Username, a.Provider);
			
		}

		// Prompt the user to add a new account
		private void OnAddAccount(object o, EventArgs args) {
			var dialog = new AccountDialog();
			if(dialog.Run() == (int)ResponseType.Accept) {
				// it's not already there
				if(!_accountList.Contains(dialog.Result)) {
					_accountList.Add(dialog.Result);
					AddToStore(dialog.Result);
				}
				else { // whoops
					using(var md = new MessageDialog(null, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.Ok,
						"That account already exists.")) {
						md.WindowPosition = WindowPosition.Center;
						md.Run();
						md.Destroy();
					}
				}
			}
			dialog.Destroy();
		}
		
		// We want to edit an entry!
		private void EditAccount(TreeModel model, TreeIter i) {
			var a = _accountList.First(x =>
				(string) model.GetValue(i, (int) Columns.Username) == x.Username &&
				(string) model.GetValue(i, (int) Columns.Service) == x.Provider);
			var dialog = new AccountDialog(a.Username, a.Credentials, a.Provider, GetDefaultAccounts(a));
			if(dialog.Run() == (int)ResponseType.Accept) {
				a.Credentials.Password = dialog.Result.Credentials.Password;
				if(dialog.DefaultAnime) _defAnime = a;
				else if(_defAnime == a) _defAnime = null; // We have deselected this account as the default!
				if(dialog.DefaultManga) _defManga = a;
				else if(_defManga == a) _defManga = null; // We have deselected this account asthe default!
				model.SetValue(i, (int)Columns.Used, GetDefaultAccounts(a));
			}
			dialog.Destroy();
		}

		private void OnRowActivated(object o, RowActivatedArgs args) { // Through row click
			var model = ((TreeView) o).Model;
			TreeIter i;

			if(!model.GetIter(out i, args.Path)) return;
			EditAccount(model, i);
		}

		private void OnEditClick(object o, EventArgs args) { // Through button
			var s = _acctTree.Selection;
			TreeIter i;
			s.GetSelected(out i);
			EditAccount(_acctTree.Model, i);
		}

		private void OnDelete(object o, DeleteEventArgs args) {
			if(_forced) {
				Application.Quit();
				Environment.Exit(0);
			}
		}

	}
}