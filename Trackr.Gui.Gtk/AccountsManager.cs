using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using Trackr.Core;

namespace Trackr.Gui.Gtk {
	/// <summary>
	/// The frame used by the settings dialog to manage accounts.
	/// </summary>
	internal class AccountsManager : Frame {
		// Table columns
		private enum Columns {
			Used,
			Username,
			Service
		}

		// widgets
		private ListStore _store;
		private TreeView _tree;
		private ScrolledWindow _sw;
		private VBox _vBox;
		private HBox _buttons;
		private Alignment _bAlign;
		private Label _label;
		private Button _add, _edit, _rem;

		// values to update
		internal List<Account> AccountList;
		internal Account DefAnime;
		internal Account DefManga;

		internal AccountsManager() {
			Instantiate();
			Build();
		}

		private void Instantiate() {
			_vBox = new VBox(false, 3);
			_store = new ListStore(typeof(string), typeof(string), typeof(string));
			_tree = new TreeView(_store);
			_sw = new ScrolledWindow();
			_label = new Label("Choose the accounts to use below. Double click on an account to change account settings.");
			_buttons = new HBox(true, 3);
			_bAlign = new Alignment(1, 1, 0, 0);
			_add = new Button(new Image(Stock.Add, IconSize.Button));
			_edit = new Button(new Image(Stock.Edit, IconSize.Button));
			_rem = new Button(new Image(Stock.Remove, IconSize.Button));
		}

		private void Build() {
			_sw.ShadowType = ShadowType.EtchedIn;
			_sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			_vBox.BorderWidth = 10;
			
			// tree columns
			var i = 0;
			foreach(var x in Enum.GetNames(typeof(Columns))) {
				_tree.AppendColumn(x, new CellRendererText(), "text", i);
				i++;
			}
			
			Add(_vBox);
			_vBox.PackStart(_label, false, false, 10);
			_vBox.Add(_sw);
			_sw.Add(_tree);
			_tree.RowActivated += OnRowActivated;
			_buttons.Add(_add);
			_add.Clicked += OnAddAccount;
			_buttons.Add(_edit);
			_edit.Clicked += OnEditClick;
			_buttons.Add(_rem);
			_rem.Clicked += OnRemoveClick;
			_bAlign.Add(_buttons);
			_vBox.PackEnd(_bAlign, false, false, 0);
		}

		// get a string representing the default accounts
		private string GetDefaultAccounts(Account a) {
			var defAccount = string.Empty;
			if(DefAnime == a)
				defAccount += "A";
			if(DefManga == a)
				defAccount += "M";
			return defAccount;
		}
		
		// Add the account to the table
		private void AddToStore(Account a) { _store.AppendValues(GetDefaultAccounts(a), a.Username, a.Provider); }

		/// <summary>
		/// Fill the Accounts frame.
		/// </summary>
		internal void Fill() {
			AccountList = new List<Account>(Program.Settings.Accounts.Count);
			Program.Settings.Accounts.ForEach(x => AccountList.Add(new Account(x)));
			DefAnime = Program.Settings.DefaultAnime == null ? null : new Account(Program.Settings.DefaultAnime);
			DefManga = Program.Settings.DefaultManga == null ? null : new Account(Program.Settings.DefaultManga);
			AccountList.ForEach(AddToStore);
		}
		
		// We want to edit an entry!
		private void EditAccount(TreeModel model, TreeIter i) {
			var a = AccountList.First(x =>
				(string) model.GetValue(i, (int) Columns.Username) == x.Username &&
				(string) model.GetValue(i, (int) Columns.Service) == x.Provider);
			var dialog = new AccountDialog(a, GetDefaultAccounts(a));
			if(dialog.Run() == (int)ResponseType.Accept) {
				a.Credentials.Password = dialog.Result.Credentials.Password;
				if(dialog.DefaultAnime) DefAnime = a;
				else if(DefAnime == a) DefAnime = null; // We have deselected this account as the default!
				if(dialog.DefaultManga) DefManga = a;
				else if(DefManga == a) DefManga = null; // We have deselected this account asthe default!
				model.SetValue(i, (int)Columns.Used, GetDefaultAccounts(a));
			}
			dialog.Destroy();
		}
		
		// Prompt the user to add a new account
		private void OnAddAccount(object o, EventArgs args) {
			var dialog = new AccountDialog();
			if(dialog.Run() == (int)ResponseType.Accept) {
				// it's not already there
				if(!AccountList.Contains(dialog.Result)) {
					AccountList.Add(dialog.Result);
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
		
		// Edit account through row click
		private void OnRowActivated(object o, RowActivatedArgs args) { 
			var model = ((TreeView) o).Model;
			TreeIter i;

			if(model.GetIter(out i, args.Path))
				EditAccount(model, i);
		}
		
		// Edit account through button press
		private void OnEditClick(object o, EventArgs args) { 
			var s = _tree.Selection;
			TreeIter i;
			
			if(s.GetSelected(out i))
				EditAccount(_tree.Model, i);
		}
		
		// Remove account through button press
		private void OnRemoveClick(object o, EventArgs args) {
			var m = _tree.Model;
			var s = _tree.Selection;
			TreeIter i;
			if(!s.GetSelected(out i)) return;

			var a = AccountList.First(x =>
				x.Provider == (string) m.GetValue(i, (int) Columns.Service) &
				x.Username == (string) m.GetValue(i, (int) Columns.Username));

			using(var md = new MessageDialog(null, DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.YesNo,
				"Really delete " + (string)m.GetValue(i, (int)Columns.Username) + "?")) {
				md.WindowPosition = WindowPosition.Center;
				var res = md.Run();
				md.Destroy();
				if(res != (int) ResponseType.Yes) return;
			}

			if(DefAnime == a) DefAnime = null;
			if(DefManga == a) DefManga = null;
			AccountList.Remove(a);
			_store.Remove(ref i);
		}
	}
}