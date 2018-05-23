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

		private void Enable() {
			_locked = false;
			Submit.Sensitive = true;
		}

		private void Disable() {
			_locked = true;
			Submit.Sensitive = false;
		}

	}
}