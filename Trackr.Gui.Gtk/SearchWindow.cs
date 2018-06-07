using System;
using Gtk;

using Trackr.Api;

namespace Trackr.Gui.Gtk {	
	internal class SearchWindow : VBox {
		protected HBox _searchControls;
		internal Entry SearchBox;
		internal Button Submit;
		protected Toolbar _toolbar;
		protected ToolButton InfoItem, AddItem, EditItem, SyncItem, SettingsItem;
		
		internal SearchWindow() : base(false, 0) {
			Instantiate();
			Build();
		}

		private void Instantiate() {
			_searchControls = new HBox(false, 0);
			SearchBox = new Entry();
			Submit = new Button(new Image(Stock.Find, IconSize.Button));
			_toolbar = new Toolbar();
			InfoItem = new ToolButton(Stock.Info);
			AddItem = new ToolButton(Stock.Add);
			EditItem = new ToolButton(Stock.Edit);
			SyncItem = new ToolButton(Stock.Refresh);
			SettingsItem = new ToolButton(Stock.Preferences);
		}

		private void Build() {
			_searchControls.PackStart(SearchBox, true, true, 10);
			_searchControls.PackEnd(Submit, false, false, 10);

			_toolbar.ToolbarStyle = ToolbarStyle.Icons;
			_toolbar.Add(InfoItem);
			_toolbar.Add(AddItem);
			_toolbar.Add(EditItem);
			_toolbar.Add(SyncItem);
			_toolbar.Add(SettingsItem);
			
			PackStart(_searchControls, false, false, 5);
			PackEnd(_toolbar, false, false, 0);
		}
		
		
	}
}