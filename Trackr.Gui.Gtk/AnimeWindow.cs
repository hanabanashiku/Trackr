using System;
using Gtk;
using Trackr.Api;
using Trackr.List;

namespace Trackr.Gui.Gtk {
	internal class AnimeWindow : VBox {
		private Notebook _nb;
		private AnimeTreeView _watchingTree, _completedTree, _holdTree, _plannedTree, _droppedTree;
		private Toolbar _toolbar;
		private ToolButton _infoItem, _editItem, _removeItem, _syncItem;
		internal ToolButton SettingsItem;
		internal Entry FilterEntry; // note: This as written will mean that searches won't carry over through different tabs

		internal AnimeWindow() : base(false, 0) {
			Instantiate();
			Build();
			Console.WriteLine("Hello world");
		}

		private void Instantiate() {
			// Notebook
			_nb = new Notebook();
			
			// Trees
			_watchingTree = new AnimeTreeView(this);
			_completedTree = new AnimeTreeView(this);
			_holdTree = new AnimeTreeView(this);
			_plannedTree = new AnimeTreeView(this);
			_droppedTree = new AnimeTreeView(this);
			
			// Toolbar
			_toolbar = new Toolbar();
			_infoItem = new ToolButton(Stock.Info);
			_editItem = new ToolButton(Stock.Edit);
			_removeItem = new ToolButton(Stock.Remove);
			_syncItem = new ToolButton(Stock.Refresh);
			SettingsItem = new ToolButton(Stock.Preferences);
			FilterEntry = new Entry();

		}

		private void Build() {
			PackStart(_nb, true, true, 5);
			
			// Watching page
			var sw = new ScrolledWindow();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.Add(_watchingTree);
			_nb.AppendPage(sw, new Label("Watching"));
			
			// Completed page
			sw = new ScrolledWindow();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.Add(_completedTree);
			_nb.AppendPage(sw, new Label("Completed"));
			
			// Hold page
			sw = new ScrolledWindow();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.Add(_holdTree);
			_nb.AppendPage(sw, new Label("On Hold"));
			
			// Planned page
			sw = new ScrolledWindow();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.Add(_plannedTree);
			_nb.AppendPage(sw, new Label("Planned"));
			
			// Dropped page
			sw = new ScrolledWindow();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.Add(_droppedTree);
			_nb.AppendPage(sw, new Label("Dropped"));
			
			PackEnd(_toolbar, false, false, 0);
			_toolbar.ToolbarStyle = ToolbarStyle.Icons;
			_toolbar.Add(_infoItem);
			_infoItem.TooltipText = "View Title Information";
			_toolbar.Add(_editItem);
			_editItem.TooltipText = "Edit Title Information";
			_toolbar.Add(_removeItem);
			_removeItem.TooltipText = "Remove Title";
			_toolbar.Add(_syncItem);
			_syncItem.TooltipText = "Synchronize List With Server";
			_toolbar.Add(SettingsItem);
			SettingsItem.TooltipText = "Change Application Settings";
			//TODO anything other than this
			for(var x = 0; x < 22; x++) _toolbar.Add(new SeparatorToolItem(){Draw = false});
			_toolbar.Add(new ToolItem(){FilterEntry});
			FilterEntry.Changed += OnFilterChanged;
			FilterEntry.Activated += OnFilterActivated;
		} // build

		internal void Fill() {
			Console.WriteLine("Clearing...");
			_watchingTree.Store.Clear();
			_completedTree.Store.Clear();
			_droppedTree.Store.Clear();
			_plannedTree.Store.Clear();
			_holdTree.Store.Clear();
			
			Console.WriteLine(Program.AnimeList == null);
			if(Program.AnimeList == null) return;
			Console.WriteLine("Filling current");
			Program.AnimeList[ApiEntry.ListStatuses.Current].ForEach(x => _watchingTree.Store.AppendValues(x));
			Console.WriteLine("Filling Completed");
			Program.AnimeList[ApiEntry.ListStatuses.Completed].ForEach(x => _completedTree.Store.AppendValues(x));
			Console.WriteLine("Filling Dropped");
			Program.AnimeList[ApiEntry.ListStatuses.Dropped].ForEach(x => _droppedTree.Store.AppendValues(x));
			Console.WriteLine("Filling Planned");
			Program.AnimeList[ApiEntry.ListStatuses.Planned].ForEach(x => _plannedTree.Store.AppendValues(x));
			Console.WriteLine("Filling hold");
			Program.AnimeList[ApiEntry.ListStatuses.OnHold].ForEach(x => _holdTree.Store.AppendValues(x));
		}

		private void OnFilterChanged(object o, EventArgs args) {
			_watchingTree.Filter.Refilter();
			_completedTree.Filter.Refilter();
			_holdTree.Filter.Refilter();
			_plannedTree.Filter.Refilter();
			_droppedTree.Filter.Refilter();
		}

		private void OnFilterActivated(object o, EventArgs args) {
			//TODO Switch to Search tab, move search results
		}
	}
}