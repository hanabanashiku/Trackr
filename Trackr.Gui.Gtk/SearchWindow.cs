using System;
using Gtk;

using Trackr.Api;

namespace Trackr.Gui.Gtk {	
	internal class SearchWindow : VBox {
		protected HBox _searchControls;
		protected Entry _searchBox;
		protected Button _submit;
		protected TreeView _treeView;
		protected Toolbar _toolbar;
		protected ToolButton _infoItem, _addItem, _editItem, _syncItem, _settingsItem;
		
		internal SearchWindow() : base(false, 0) {
			Instantiate();
			Build();
		}

		private void Instantiate() {
			_searchControls = new HBox(false, 0);
			_searchBox = new Entry();
			_submit = new Button(new Image(Stock.Find, IconSize.Button));
			_toolbar = new Toolbar();
			_infoItem = new ToolButton(Stock.Info);
			_addItem = new ToolButton(Stock.Add);
			_editItem = new ToolButton(Stock.Edit);
			_syncItem = new ToolButton(Stock.Refresh);
			_settingsItem = new ToolButton(Stock.Preferences);
		}

		private void Build() {
			_searchControls.PackStart(_searchBox, true, true, 10);
			_searchControls.PackEnd(_submit, false, false, 10);

			_toolbar.ToolbarStyle = ToolbarStyle.Icons;
			_toolbar.Add(_infoItem);
			_toolbar.Add(_addItem);
			_toolbar.Add(_editItem);
			_toolbar.Add(_syncItem);
			_toolbar.Add(_settingsItem);
			
			PackStart(_searchControls, false, false, 5);
			PackEnd(_toolbar, false, false, 0);
		}
	}
}