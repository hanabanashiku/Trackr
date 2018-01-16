using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Gdk;
using Gtk;
using Window = Gtk.Window;

namespace Trackr.Gui.Gtk {
	public class SettingsWindow : Window {

		private readonly bool _forced;

		private VBox _container;
		private Notebook _nb;
		private Frame _general, _accounts, _recognition, _torrents;
		private HBox _buttons;
		private Alignment _hAlign;

		private Button _ok, _cancel;
		
		/// <summary>
		/// Spawn a new Settings dialog box
		/// </summary>
		/// <param name="forced">Set to true if there was no settings definiton beforehand.</param>
		public SettingsWindow(bool forced) : base("Settings") {
			_forced = forced;
			DefaultSize = new Size(500, 450);
			DestroyWithParent = true;
			Role = "settings";
			WindowPosition = _forced ? WindowPosition.Center : WindowPosition.CenterOnParent;

			Instantiate();
			Build();
			
			// Events
			DeleteEvent += OnDelete;
			ShowAll();
		}

		private void Instantiate() {
			_container = new VBox(false, 3);
			_nb = new Notebook();
			_general = new Frame();
			_accounts = new Frame();
			_recognition = new Frame();
			_torrents = new Frame();
			_buttons = new HBox(true, 3);
			_hAlign = new Alignment(1, 1, 0, 0);

			_ok = new Button("OK");
			_cancel = new Button("Cancel");

		}

		private void Build() {
			Add(_container);
			_container.PackStart(_nb, true, true, 0);
			_nb.InsertPage(_general, new Label("General"), -1);
			_general.Add(new Label("Hello World"));
			_nb.InsertPage(_accounts, new Label("Accounts"), -1);
			_nb.InsertPage(_recognition, new Label("Recognition"), -1);
			_nb.InsertPage(_torrents, new Label("Torrents"), -1);
			
			
			_container.PackEnd(_hAlign, false, false, 0);
			_hAlign.Add(_buttons);
			_buttons.Add(_ok);
			_ok.SetSizeRequest(70, 30);
			_buttons.Add(_cancel);
			_cancel.Clicked += delegate { Destroy(); };

		}

		private void OnDelete(object o, DeleteEventArgs args) {
			if(_forced)
				Application.Quit();
		}
	}
}