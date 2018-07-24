using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gtk;
using Trackr.Api;

namespace Trackr.Gui.Gtk {
	internal class AnimeSearchWindow : SearchWindow {
		private readonly AnimeSearchTreeView _treeView;
		private bool _changed;
		private bool _locked;

		internal AnimeSearchWindow() {
			_treeView = new AnimeSearchTreeView();
			var sw = new ScrolledWindow() { _treeView };
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			Add(sw);
			SearchBox.Changed += OnChanged;
			SearchBox.Activated += OnSubmit;
			Submit.Clicked += OnSubmit;
			_treeView.Visible = false;
			AddItem.Clicked += OnAdd;
			EditItem.Clicked += OnEdit;
			_treeView.Selection.Changed += OnSelectionChanged;
		}

		private async void Search(string keywords) {
			if(_locked) return; // some thread safety
			Disable();
			
			if(Program.AnimeList == null) return;
			try {
				var results = Program.AnimeList.Find(keywords);
				Fill(await results);
			}
			catch(Exception e) {
				var d = new MessageDialog(null, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.Ok, $"Search Error: {e.Message}");
				d.Run();
				d.Destroy();
				_treeView.Store.Clear();
			}

			Enable();
		}

		private void Fill(List<Anime> results) {
			_treeView.Store.Clear();
			
			foreach(var a in results)
				_treeView.Store.AppendValues(a);
		}

		internal void Refresh() {
			_treeView.Hide();
			_treeView.Show();
		}

		private void OnChanged(object o, EventArgs args) {
			_changed = true;
		}
		
		private async void OnSubmit(object o, EventArgs args) {
			// no unnecessary requests
			if(SearchBox.Text == "" || !_changed) return;

			_treeView.Visible = true;
			await Task.Run(() => Search(SearchBox.Text));
			_changed = false;
		}

		// Enable searching (request has completed)
		private void Enable() {
			_locked = false;
			Submit.Sensitive = true;
		}

		// Disable searching (request in progress)
		private void Disable() {
			_locked = true;
			Submit.Sensitive = false;
		}

		// User clicked the add button
		private void OnAdd(object o, EventArgs args) {
			if(_treeView.Selection.CountSelectedRows() == 0) return;
			_treeView.Selection.GetSelected(out var i);
			var a = (Anime)_treeView.Store.GetValue(i, 0);
			if(a.ListStatus != ApiEntry.ListStatuses.NotInList) return;

			a.ListStatus = ApiEntry.ListStatuses.Current;
			a.UserStart = DateTime.Today;
			Program.AnimeList.Add(a);
			Program.Win.AnimeBox.WatchingTree.Store.AppendValues(a);

			AddItem.Visible = false;
			Refresh();
		}

		private void OnSelectionChanged(object o, EventArgs args) {
			if(_treeView.Selection.CountSelectedRows() == 0) {
				EditItem.Sensitive = false;
                AddItem.Sensitive = false;
                InfoItem.Sensitive = false;
			}
			else {
				EditItem.Sensitive = true;
				AddItem.Sensitive = true;
				InfoItem.Sensitive = true;
				_treeView.Selection.GetSelected(out var i);
				var a = (ApiEntry)_treeView.Store.GetValue(i, 0);
				AddItem.Visible = a.ListStatus == ApiEntry.ListStatuses.NotInList;
			}
		}

		private void OnEdit(object o, EventArgs args) {
			if(_treeView.Selection.CountSelectedRows() == 0) return;
			_treeView.ActivateRow(_treeView.Selection.GetSelectedRows()[0], _treeView.GetColumn(0));
		}

	}
}