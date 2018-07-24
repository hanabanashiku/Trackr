using System;
using Gtk;
using Trackr.Api;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Trackr.Gui.Gtk {
	internal class AnimeWindow : VBox {
		private Notebook _nb;
		internal AnimeTreeView WatchingTree, CompletedTree, HoldTree, PlannedTree, DroppedTree;
		private Toolbar _toolbar;
		private ToolButton _infoItem, _editItem, _removeItem;
		internal ToolButton SettingsItem, SyncItem;
		private Fixed _entryFixed;
		internal Entry FilterEntry; // note: This as written will mean that searches won't carry over through different tabs
		internal AnimeTreeView[] Views;

		internal AnimeWindow() : base(false, 0) {
			Instantiate();
			Build();
		}

		private void Instantiate() {
			// Notebook
			_nb = new Notebook();
			
			// Trees
			WatchingTree = new AnimeTreeView(this);
			CompletedTree = new AnimeTreeView(this);
			HoldTree = new AnimeTreeView(this);
			PlannedTree = new AnimeTreeView(this);
			DroppedTree = new AnimeTreeView(this);
			Views = new [] {null, WatchingTree, CompletedTree, HoldTree, DroppedTree, PlannedTree};
			
			// Toolbar
			_toolbar = new Toolbar();
			_infoItem = new ToolButton(Stock.Info);
			_editItem = new ToolButton(Stock.Edit);
			_removeItem = new ToolButton(Stock.Remove);
			SyncItem = new ToolButton(Stock.Refresh);
			SettingsItem = new ToolButton(Stock.Preferences);
			
			_entryFixed = new Fixed();
			FilterEntry = new Entry();

		}

		private void Build() {
			PackStart(_nb, true, true, 5);
			
			// Watching page
			var sw = new ScrolledWindow();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.Add(WatchingTree);
			WatchingTree.Selection.Changed += OnSelectionChanged;
			_nb.AppendPage(sw, new Label("Watching"));
			
			// Completed page
			sw = new ScrolledWindow();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.Add(CompletedTree);
			CompletedTree.Selection.Changed += OnSelectionChanged;
			_nb.AppendPage(sw, new Label("Completed"));
			
			// Hold page
			sw = new ScrolledWindow();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.Add(HoldTree);
			HoldTree.Selection.Changed += OnSelectionChanged;
			_nb.AppendPage(sw, new Label("On Hold"));
			
			// Planned page
			sw = new ScrolledWindow();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.Add(PlannedTree);
			PlannedTree.Selection.Changed += OnSelectionChanged;
			_nb.AppendPage(sw, new Label("Planned"));
			
			// Dropped page
			sw = new ScrolledWindow();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.Add(DroppedTree);
			DroppedTree.Selection.Changed += OnSelectionChanged;
			_nb.AppendPage(sw, new Label("Dropped"));
			
			PackEnd(_toolbar, false, false, 0);
			_toolbar.ToolbarStyle = ToolbarStyle.Icons;
			_toolbar.Add(_infoItem);
			_infoItem.TooltipText = "View Title Information";
			_infoItem.Sensitive = false;
			_toolbar.Add(_editItem);
			_editItem.TooltipText = "Edit Title Information";
			_editItem.Sensitive = false;
			_editItem.Clicked += OnEdit;
			_toolbar.Add(_removeItem);
			_removeItem.TooltipText = "Remove Title";
			_removeItem.Sensitive = false;
			_removeItem.Clicked += OnRemove;
			_toolbar.Add(SyncItem);
			SyncItem.TooltipText = "Synchronize List With Server";
			_toolbar.Add(SettingsItem);
			SettingsItem.TooltipText = "Change Application Settings";
			
			_toolbar.Add(new ToolItem(){_entryFixed});
			_entryFixed.Put(FilterEntry, 250, 10);
			FilterEntry.Changed += OnFilterChanged;
			FilterEntry.Activated += OnFilterActivated;

			_nb.SwitchPage += delegate { OnSelectionChanged(_nb, EventArgs.Empty); };
		} // build

		internal void Fill() {
			WatchingTree.Store.Clear();
			CompletedTree.Store.Clear();
			DroppedTree.Store.Clear();
			PlannedTree.Store.Clear();
			HoldTree.Store.Clear();
			
			if(Program.AnimeList == null) return;
			Program.AnimeList[ApiEntry.ListStatuses.Current].ForEach(x => WatchingTree.Store.AppendValues(x));
			Program.AnimeList[ApiEntry.ListStatuses.Completed].ForEach(x => CompletedTree.Store.AppendValues(x));
			Program.AnimeList[ApiEntry.ListStatuses.Dropped].ForEach(x => DroppedTree.Store.AppendValues(x));
			Program.AnimeList[ApiEntry.ListStatuses.Planned].ForEach(x => PlannedTree.Store.AppendValues(x));
			Program.AnimeList[ApiEntry.ListStatuses.OnHold].ForEach(x => HoldTree.Store.AppendValues(x));
		}

		// display changes
		internal void Refresh() {
			for(var j = 1; j < Views.Length; j++) {
				var v = Views[j];
				v.Hide();
				v.Show();
			}
		}


		private void OnFilterChanged(object o, EventArgs args) {
			WatchingTree.Filter.Refilter();
			CompletedTree.Filter.Refilter();
			HoldTree.Filter.Refilter();
			PlannedTree.Filter.Refilter();
			DroppedTree.Filter.Refilter();
		}

		private void OnFilterActivated(object o, EventArgs args) {
			Program.Win.SwitchTab(MainWindow.Page.AnimeSearch);
			Program.Win.AnimeSearch.SearchBox.Text = FilterEntry.Text;
			FilterEntry.Text = "";
			Program.Win.AnimeSearch.Submit.Click();
		}

		// Get the tree matching the current tab
		private AnimeTreeView GetCurrentTree() {
			switch(_nb.CurrentPage) {
				case 0: // Watching
					return WatchingTree;
				case 1: // Completed
					return CompletedTree;
				case 2: // Hold
					return HoldTree;
				case 3: // Planned
					return PlannedTree;
				case 4: // Dropped
					return DroppedTree;
				default: return null;
					
			}
		}

		private void OnSelectionChanged(object o, EventArgs args) {
			if(GetCurrentTree().Selection.CountSelectedRows() == 0) {
				_infoItem.Sensitive = false;
				_editItem.Sensitive = false;
				_removeItem.Sensitive = false;
			}
			else {
				_infoItem.Sensitive = true;
				_editItem.Sensitive = true;
				_removeItem.Sensitive = true;
			}
		}

		// The edit button is clicked - same as double clicking on an entry
		private void OnEdit(object o, EventArgs args) {
			var v = GetCurrentTree();
			if(v == null || v.Selection.CountSelectedRows() == 0) return;
			
			foreach(var s in v.Selection.GetSelectedRows())
				v.ActivateRow(s, v.GetColumn(0));
		}

		private void OnRemove(object o, EventArgs args) {
			var v = GetCurrentTree();
			if(v == null || v.Selection.CountSelectedRows() == 0) return;

			var d = new MessageDialog(Program.Win, DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.YesNo, "Delete the selected media?");
			var res = (ResponseType)d.Run();
			d.Destroy();
			if(res != ResponseType.Yes) return;
			
			foreach(var s in v.Selection.GetSelectedRows()) {
				v.Store.GetIter(out var i, s);
				var a = (Anime)v.Store.GetValue(i, 0);
				Program.AnimeList.Remove(a);
				v.Store.Remove(ref i);
			}
			
			Program.Win.RefreshAnimeLists();
		}

		// Sync the list with the store
		internal void Sync() {
			if(Program.AnimeList == null) return;

			var seen = new List<Anime>();

			for(var n = 1; n < Views.Length; n++) {
				var store = Views[n].Store;
				TreeIter i;
				store.GetIterFirst(out i);

				for(var j = 0; j < store.IterNChildren(); j++){ // cycle over each ListStore, move everything to the correct place or remove it
					var a = (Anime)store.GetValue(i, 0);
					seen.Add(a);

					// Shouldn't be here at all
					if(a.ListStatus == ApiEntry.ListStatuses.NotInList) store.Remove(ref i);
					// Different status, move it.
					else if(a.ListStatus != (ApiEntry.ListStatuses)n) {
						store.Remove(ref i);
						Views[(int)a.ListStatus].Store.AppendValues(a);
					}
					store.IterNext(ref i);
				}
			}

			// Add whatever we haven't seen yet.
			foreach(var a in Program.AnimeList.Except(seen))
				Views[(int)a.ListStatus].Store.AppendValues(a);

			// refresh lists without having to click (values updated automatically - OOP magic)
			for(var i = 1; i < Views.Length; i++) {
				Views[i].Hide();
				Views[i].Show();
			}
		}
	}
}