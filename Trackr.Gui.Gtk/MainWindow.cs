﻿using System;
using System.Diagnostics;
using System.IO;
using Gdk;
using Gtk;
using Trackr.List;
using Image = Gtk.Image;
using Window = Gtk.Window;

namespace Trackr.Gui.Gtk {
	public class MainWindow : Window {
		private VBox _container;
		private MenuBar _menu;
		private Menu _fileMenu, _helpMenu;
		private MenuItem _file, _help, _close, _quit, _sync, _settings, _about, _updates;
		private HPaned _pane;
		private TreeView _sidebar;
		private TreeViewColumn _column;
		private TreeStore _store;
		private Notebook _nb;
		private AnimeWindow _animeBox;
		private NullAccountWindow _nullAccountBox;
		private VBox _mangaBox, _searchBox;
		internal Statusbar _statusbar;
		
		private enum Page { Anime = 0, Manga = 1, NullAccount = 2, Search = 3 }
		
		public MainWindow() : base(Program.AppName) {
			Icon = Program.AppIcon;
			DefaultSize = new Size(700, 550);
			Role = "MainWindow";
			WindowPosition = WindowPosition.Center;
			KeepAbove = Program.Settings.KeepWindowOnTop;
			
			Instantiate();
			Build();

			DeleteEvent += OnDelete;
			ShowAll();
			
			if(Program.AnimeList != null) _nb.CurrentPage = (int)Page.Anime;
			else if (Program.MangaList != null) _nb.CurrentPage = (int) Page.Manga;
			else _nb.CurrentPage = (int) Page.NullAccount;
		}

		private void Instantiate() {
			// Containers
			_container = new VBox(false, 3);
			_menu = new MenuBar();
			_statusbar = new Statusbar();

			// Menu bar
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

			// Sidebar
			_pane = new HPaned();
			_store = new TreeStore(typeof(Pixbuf), typeof(string));
			_sidebar = new TreeView(_store);
			_column = new TreeViewColumn();
			
			// Notebook
			_nb = new Notebook();
			_animeBox = new AnimeWindow();
			_mangaBox = new VBox();
			_nullAccountBox = new NullAccountWindow();
			_searchBox = new VBox();
			

		}

		private void Build() {
			_container.PackStart(_menu, false, false, 0);
			_container.Add(_pane);
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
			
			// Sidebar
			_pane.Add1(_sidebar);
			_sidebar.CursorChanged += OnSidebarActivated;
			var crp = new CellRendererPixbuf();
			var crt = new CellRendererText();
			_column.PackStart(crp, true);
			_column.PackEnd(crt, true);
			_column.AddAttribute(crp, "pixbuf", 0);
			_column.AddAttribute(crt, "text", 1);
			_sidebar.AppendColumn(_column);
			_sidebar.HeadersVisible = false;
			//TODO find icons
			_store.AppendValues(null, "Anime");
			_store.AppendValues(null, "Manga");
			_store.AppendValues(null, "Search");

			// Notebook
			_pane.Add2(_nb);
			_nb.ShowTabs = false;
			_nb.Add(_animeBox);
			_nb.Add(_mangaBox);
			_nb.Add(_nullAccountBox);
			_nb.Add(_searchBox);

			_animeBox.SettingsItem.Clicked += OnSettings;
		}

		internal void Fill() {
			_animeBox.Fill();
			//_mangaBox.Fill();
		}

		private void OnSidebarActivated(object o, EventArgs args) {
			var s = _sidebar.Selection;
			TreeIter i;
			if(!s.GetSelected(out i)) return;
			switch((string)_sidebar.Model.GetValue(i, 1)) {
				case "Anime":
					// Switch to the null account page or the anime page
					_nb.CurrentPage = Program.AnimeList == null ? (int)Page.NullAccount : (int)Page.Anime;
					break;
				case "Manga":
					// Switch to the null account page or the manga page
					_nb.CurrentPage = (Program.MangaList == null) ? (int)Page.NullAccount : (int)Page.Manga;
					break;
				case "Search":
					_nb.CurrentPage = (int)Page.Search;
					break;
				default:
					Debug.WriteLine("Warning: Unknown page");
					break;
			}
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