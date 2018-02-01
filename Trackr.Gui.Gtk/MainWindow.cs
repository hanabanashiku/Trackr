using System;
using System.Net;
using System.Text;
using Gtk;

namespace Trackr.Gui.Gtk {
	public class MainWindow : Window {
		private VBox _container;
		private HPaned _paned;
		private MenuBar _menu;
		private Menu _fileMenu, _helpMenu;
		private MenuItem _file, _help, _close, _quit, _sync, _settings, _about, _updates;
		private Statusbar _statusbar;
		
		public MainWindow() : base(Program.AppName) {
			Icon = Program.AppIcon;
			DefaultSize = new Gdk.Size(600, 550);
			Role = "MainWindow";
			WindowPosition = WindowPosition.Center;
			KeepAbove = Program.Settings.KeepWindowOnTop;
			
			Instantiate();
			Build();

			DeleteEvent += OnDelete;
			ShowAll();
		}

		private void Instantiate() {
			_container = new VBox(false, 3);
			_paned = new HPaned();
			_menu = new MenuBar();
			_statusbar = new Statusbar();

			_fileMenu = new Menu();
			_helpMenu = new Menu();
			_file = new MenuItem("File");
			_help = new MenuItem("Help");
			_sync = new MenuItem("Synchronize");
			_settings = new MenuItem("Preferences");
			_close = new MenuItem("Close");
			_quit = new MenuItem("Quit");
			_about = new MenuItem("About " + Program.AppName);
			_updates = new MenuItem("Check for Updates");
		}

		private void Build() {
			_container.PackStart(_menu, false, false, 0);
			_container.Add(_paned);
			_container.PackEnd(_statusbar, false, false, 0);
			Add(_container);
			
			// Menubar
			_fileMenu.Add(_sync);
			_fileMenu.Add(_settings);
			_settings.Activated += OnSettings;
			_fileMenu.Add(_close);
			_close.Activated += delegate { Visible = false; };
			_fileMenu.Add(new SeparatorMenuItem());
			_fileMenu.Add(_quit);
			_quit.Activated += delegate { Application.Quit(); };
			_menu.Add(_file);
			_file.Submenu = _fileMenu;
			_helpMenu.Add(_updates);
			_helpMenu.Add(_about);
			_about.Activated += OnAbout;
			_menu.Add(_help);
			_help.Submenu = _helpMenu;

		}

		internal void OnSettings(object o, EventArgs args) {
			var s = new SettingsWindow(false);
			if(s.Run() == (int) ResponseType.Accept) {
				Program.SettingsChanged();
				//TODO: Change accounts shown if the default accounts have been changed
			}
			s.Destroy();
		}

		private void OnAbout(object o, EventArgs args) {
			var d = new AboutDialog {
				ProgramName = Program.AppName,
				Version = Program.AppVersion,
				Logo = Program.AppIcon,
				Comments = "Trackr is a simple program for tracking and managing your watched anime and manga.",
				Website = "https://github.com/beesenpai/Trackr",
				TransientFor = this,
				WindowPosition = WindowPosition.CenterOnParent
			};
			d.Run();
			d.Destroy();
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