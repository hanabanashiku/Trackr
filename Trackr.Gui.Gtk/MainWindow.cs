﻿﻿using System;
using System.Diagnostics;
 using System.Threading.Tasks;
 using Gdk;
using Gtk;
 using Trackr.Api;
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
		internal AnimeWindow AnimeBox;
		private NullAccountWindow _nullAccountBox;
		private VBox _mangaBox, _defaultSearch;
		internal AnimeSearchWindow AnimeSearch;
		private SearchWindow _mangaSearch;
		internal Statusbar Statusbar;
		internal Label StatusLabel;
		
		internal enum Page { Anime = 0, Manga = 1, NullAccount = 2, DefaultSearch = 3, AnimeSearch = 4, MangaSearch = 5 }
		
		public MainWindow() : base(Program.AppName) {
			Icon = Program.AppIcon;
			DefaultSize = new Size(710, 550);
			SetSizeRequest(710, 550);
			Resizable = false;
			Role = "MainWindow";
			WindowPosition = WindowPosition.Center;
			KeepAbove = Program.Settings.KeepWindowOnTop;
			
			Instantiate();
			Build();

			DeleteEvent += OnDelete;
			ShowAll();
			
			if(Program.AnimeList != null) SwitchTab(Page.Anime);
			else if (Program.MangaList != null) SwitchTab(Page.Manga);
			else SwitchTab(Page.NullAccount);
		}

		private void Instantiate() {
			// Containers
			_container = new VBox(false, 3);
			_menu = new MenuBar();
			StatusLabel = new Label();
			Statusbar = new Statusbar() {StatusLabel};

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
			AnimeBox = new AnimeWindow();
			_mangaBox = new VBox();
			_nullAccountBox = new NullAccountWindow();
			_defaultSearch = new VBox();
			AnimeSearch = new AnimeSearchWindow();
			_mangaSearch = new SearchWindow();

		}

		private void Build() {
			_container.PackStart(_menu, false, false, 0);
			_container.Add(_pane);
			_container.PackEnd(Statusbar, false, false, 0);
			Add(_container);
			
			// Menubar
			_fileMenu.Add(_sync);
			_sync.Activated += OnSync;
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
			_store.AppendValues(Pixbuf.LoadFromResource("Trackr.Gui.Gtk.Resources.icons.anime.png"), "Anime");
			_store.AppendValues(Pixbuf.LoadFromResource("Trackr.Gui.Gtk.Resources.icons.manga.png"), "Manga");
			var i = _store.AppendValues(Pixbuf.LoadFromResource("Trackr.Gui.Gtk.Resources.icons.find.png"), "Search");
			_store.AppendValues(i, Pixbuf.LoadFromResource("Trackr.Gui.Gtk.Resources.icons.search.png"), "Anime "); // the extra space means search!
			_store.AppendValues(i, Pixbuf.LoadFromResource("Trackr.Gui.Gtk.Resources.icons.search.png"), "Manga ");
			
			// Notebook
			_pane.Add2(_nb);
			_nb.ShowTabs = false;
			_nb.Add(AnimeBox);
			_nb.Add(_mangaBox);
			_nb.Add(_nullAccountBox);
			_nb.Add(_defaultSearch);
			_nb.Add(AnimeSearch);
			_nb.Add(_mangaSearch);

			// toolbar buttons
			AnimeBox.SettingsItem.Clicked += OnSettings;
			AnimeBox.SyncItem.Clicked += OnSync;
		}

		internal void SwitchTab(Page p) {
			_nb.CurrentPage = (int)p;
		}

		internal void Fill(Type t = null) {
			if(t == null || t == typeof(Anime))
				AnimeBox.Fill();
			//if(t == null || t = typeof(Manga))
			//	_mangaBox.Fill();
		}

		internal void RefreshAnimeLists() {
			AnimeBox.Refresh();
			AnimeSearch.Refresh();
		}

		private void OnSidebarActivated(object o, EventArgs args) {
			var s = _sidebar.Selection;
			TreeIter i;
			if(!s.GetSelected(out i)) return;
			switch((string)_sidebar.Model.GetValue(i, 1)) {
				case "Anime":
					// Switch to the null account page or the anime page
					SwitchTab(Program.AnimeList == null ? Page.NullAccount : Page.Anime);
					break;
				case "Manga":
					// Switch to the null account page or the manga page
					SwitchTab((Program.MangaList == null) ? Page.NullAccount : Page.Manga);
					break;
				case "Search":
					SwitchTab(Page.DefaultSearch);
					break;
				case "Anime ":
					SwitchTab((Program.AnimeList == null) ? Page.NullAccount : Page.AnimeSearch);
					break;
				case "Manga ":
					SwitchTab((Program.MangaList == null) ? Page.NullAccount : Page.MangaSearch);
					break;
				default:
					Debug.WriteLine("Warning: Unknown page");
					break;
			}
		}

		internal async void OnSync(object o, EventArgs args) {
			Statusbar.Push(1, "Syncing...");
			try {
				if(Program.AnimeList != null) {
					await Task.Run(() => Program.AnimeList.Sync());
					AnimeBox.Sync();
				}
			}
			catch(Exception e) {
				Debug.Write(e.StackTrace);
				Statusbar.Pop(1);
				Statusbar.Push(1, $"Error: {e.InnerException?.Message ?? e.Message}");
				await Task.Delay(2000);
			}
			finally {
				Statusbar.Pop(1);
			}

		}

		// Use this to trigger the settings window!
		internal void OnSettings(object o, EventArgs args) {
			var s = new SettingsWindow(false);
			if(s.Run() == (int) ResponseType.Accept) {
				Program.SettingsChanged();
				
				// Update current page if accounts have changed!
				if((Page)_nb.CurrentPage == Page.Anime && Program.AnimeList == null)
					SwitchTab(Page.NullAccount);
				else if((Page)_nb.CurrentPage == Page.Manga && Program.MangaList == null)
					SwitchTab(Page.NullAccount);
				else if((Page)_nb.CurrentPage == Page.NullAccount) {
					if(Program.AnimeList != null) SwitchTab(Page.Anime);
					else if(Program.MangaList != null) SwitchTab(Page.Manga);
				}
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