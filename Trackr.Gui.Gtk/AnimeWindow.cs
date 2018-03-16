using System;
using System.Threading.Tasks;
using Gtk;
using Trackr.Api;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Trackr.Gui.Gtk {
	internal class AnimeWindow : VBox {
		private Notebook _nb;
		internal AnimeTreeView WatchingTree, CompletedTree, HoldTree, PlannedTree, DroppedTree;
		private Toolbar _toolbar;
		private ToolButton _infoItem, _editItem, _removeItem;
		internal ToolButton SettingsItem, SyncItem;
		internal Entry FilterEntry; // note: This as written will mean that searches won't carry over through different tabs
		private AnimeTreeView[] _views;

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
			_views = new AnimeTreeView[] {null, WatchingTree, CompletedTree, HoldTree, DroppedTree, PlannedTree};
			
			// Toolbar
			_toolbar = new Toolbar();
			_infoItem = new ToolButton(Stock.Info);
			_editItem = new ToolButton(Stock.Edit);
			_removeItem = new ToolButton(Stock.Remove);
			SyncItem = new ToolButton(Stock.Refresh);
			SettingsItem = new ToolButton(Stock.Preferences);
			FilterEntry = new Entry();

		}

		private void Build() {
			PackStart(_nb, true, true, 5);
			
			// Watching page
			var sw = new ScrolledWindow();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.Add(WatchingTree);
			_nb.AppendPage(sw, new Label("Watching"));
			
			// Completed page
			sw = new ScrolledWindow();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.Add(CompletedTree);
			_nb.AppendPage(sw, new Label("Completed"));
			
			// Hold page
			sw = new ScrolledWindow();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.Add(HoldTree);
			_nb.AppendPage(sw, new Label("On Hold"));
			
			// Planned page
			sw = new ScrolledWindow();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.Add(PlannedTree);
			_nb.AppendPage(sw, new Label("Planned"));
			
			// Dropped page
			sw = new ScrolledWindow();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.Add(DroppedTree);
			_nb.AppendPage(sw, new Label("Dropped"));
			
			PackEnd(_toolbar, false, false, 0);
			_toolbar.ToolbarStyle = ToolbarStyle.Icons;
			_toolbar.Add(_infoItem);
			_infoItem.TooltipText = "View Title Information";
			_toolbar.Add(_editItem);
			_editItem.TooltipText = "Edit Title Information";
			_toolbar.Add(_removeItem);
			_removeItem.TooltipText = "Remove Title";
			_toolbar.Add(SyncItem);
			SyncItem.TooltipText = "Synchronize List With Server";
			_toolbar.Add(SettingsItem);
			SettingsItem.TooltipText = "Change Application Settings";
			//TODO anything other than this
			for(var x = 0; x < 22; x++) _toolbar.Add(new SeparatorToolItem(){Draw = false});
			_toolbar.Add(new ToolItem(){FilterEntry});
			FilterEntry.Changed += OnFilterChanged;
			FilterEntry.Activated += OnFilterActivated;
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

		private void OnFilterChanged(object o, EventArgs args) {
			WatchingTree.Filter.Refilter();
			CompletedTree.Filter.Refilter();
			HoldTree.Filter.Refilter();
			PlannedTree.Filter.Refilter();
			DroppedTree.Filter.Refilter();
		}

		private void OnFilterActivated(object o, EventArgs args) {
			//TODO Switch to Search tab, move search results
		}

		public void Sync() {
			if(Program.AnimeList == null) return;
			
			for(var i = 1; i < _views.Length; i++) {
				var store = _views[i].Store;
				var entries = Program.AnimeList[(ApiEntry.ListStatuses)i];
				var current = new List<Anime>();

				entries.ForEach(x => Debug.WriteLine(x.Title));

				// Remove everything from store thats not in the list
				TreeIter iter;
				store.GetIterFirst(out iter);
				for(var j = 0; j < store.IterNChildren(); j++) { 
					var a = (Anime)store.GetValue(iter, 0);
					if(!entries.Contains(a))
						store.Remove(ref iter);
					else current.Add(a);
					store.IterNext(ref iter);
				}

				// Add everything thats not in the store
				foreach(var a in entries.Except(current))
					store.AppendValues(a);
			}

		// refresh lists without having to click (values updated automatically - OOP magic)
		for(var i = 1; i < _views.Length; i++) {
				_views[i].Hide();
				_views[i].Show();
			}
		}
	}
}