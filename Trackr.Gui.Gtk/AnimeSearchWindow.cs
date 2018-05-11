using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gtk;
using Trackr.Api;

namespace Trackr.Gui.Gtk {
	internal class AnimeSearchWindow : SearchWindow {
		private AnimeSearchTreeView _treeView;
		private bool changed;

		internal AnimeSearchWindow() : base() {
			_treeView = new AnimeSearchTreeView();
			Add(_treeView);
			_searchBox.Changed += OnChanged;
			_submit.Clicked += OnSubmit;
			_treeView.Visible = false;
		}

		internal async void Search(string keywords) {
			if(Program.AnimeList == null) return;

			var results = await Program.AnimeList.Find(keywords);
			Fill(results);
		}

		private void Fill(List<Anime> results) {
			_treeView.Store.Clear();

			foreach(var a in results)
				_treeView.Store.AppendValues(a);
		}

		private void OnChanged(object o, EventArgs args) {
			changed = true;
		}
		
		private async void OnSubmit(object o, EventArgs args) {
			// no unnecessary requests
			if(_searchBox.Text == "" || !changed) return;
			
			_treeView.Visible = true;
			await Task.Run(() => Search(_searchBox.Text));
			changed = false;
		}

	}
}