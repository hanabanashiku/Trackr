using Gtk;

namespace Trackr.Gui.Gtk {
	public class MainWindow : Window {
		private VBox _container;
		private MenuBar _menu;
		private Statusbar _statusbar;
		
		public MainWindow() : base(Program.AppName) {
			Icon = Program.AppIcon;
			DefaultSize = new Gdk.Size(500, 650);
			Role = "MainWindow";

			Instantiate();
			Build();

			DeleteEvent += OnDelete;
			ShowAll();
		}

		private void Instantiate() {
			_container = new VBox();
			_menu = new MenuBar();
			_statusbar = new Statusbar();
		}

		private void Build() {
			_container.Add(_menu);
			_container.Add(_statusbar);
			Add(_container);
		}

		private void OnDelete(object o, DeleteEventArgs args) {
			if (Program.Settings.MinimizeToTray) {
				args.RetVal = true; // Don't destroy!
				Visible = false;
			}
			else {
				args.RetVal = false;
				Application.Quit();
			}
		}
	}
}