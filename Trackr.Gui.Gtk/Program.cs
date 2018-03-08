using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Gdk;
using Gtk;
using Trackr.Api;
using Trackr.List;
using Settings = Trackr.Core.Settings;


namespace Trackr.Gui.Gtk {
    public static class Program {
        public const string AppName = "Trackr";
        public static readonly Pixbuf AppIcon = Pixbuf.LoadFromResource("Trackr.Gui.Gtk.Resources.trackr.png");
        public const string AppVersion = "0.0";
        
        private static SystemTray _tray;
        public static MainWindow Win;
        public static Settings Settings; // Reference to Core.Program.UserSettings
        public static AnimeList AnimeList;
        public static MangaList MangaList;
        
        public static void Main(string[] args) {            
            // If the settings file doesn't exist, force the user to set up account.
            var force = !Settings.Exists;
            // This will load the settings file for us or return a new one (if force is true)
            try {
                Core.Program.UserSettings = Settings.Load();
            }
            catch (InvalidOperationException) { // Wrong version of settings, most likely. For now we will recreate
                Settings.Delete();
                Core.Program.UserSettings = Settings.Load();
            }
            finally { Settings = Core.Program.UserSettings; }
            Debug.Assert(Settings != null);
            
            Application.Init();
            if(force)
                using (var s = new SettingsWindow(true)) {
                    var res = s.Run();
                    s.Destroy();
                    if (res != (int) ResponseType.Accept)
                        Environment.Exit(0);
                }
            // Get our lists ready (and sync them)
            AnimeList = GetAnimeList();
            MangaList = GetMangaList();

            // we have a settings file! Spawn our notification icon and window
            _tray = new SystemTray();
            Win = new MainWindow { Visible = Settings.ShowWindowOnStart };
            Win.Fill();
            Application.Run();
        }

        /// <summary>
        /// Called whenever the settings object has been modified.
        /// </summary>
        public static void SettingsChanged() {
            Win.KeepAbove = Settings.KeepWindowOnTop;
            
            // Check default accounts
            if(Settings.DefaultAnime == null)
                AnimeList = null;
            else if(Settings.DefaultAnime.Provider != AnimeList.Api || Settings.DefaultAnime.Username != AnimeList.Username)
                AnimeList = GetAnimeList();
            if(Settings.DefaultManga == null)
                MangaList = null;
            else if(Settings.DefaultManga.Provider != MangaList.Api || Settings.DefaultManga.Username != MangaList.Username)
                MangaList = GetMangaList();
            
            // Reload the list - what if the changed the title type?
            Win.Fill();
        }
        

        /// <summary>
        /// Return the correct anime list based on the default value.
        /// </summary>
        /// <returns>The retrieved AnimeList</returns>
        internal static AnimeList GetAnimeList() {
            var act = Settings.DefaultAnime;
            try {
                AnimeList list = null;
                switch(act?.Provider) {
                    case "MyAnimeList":
                        list = AnimeList.Load(new MyAnimeList(act.Credentials));
                        break;
                    case "Kitsu":
                        list = AnimeList.Load(new Kitsu(act.Credentials));
                        break;
                    case "AniList":
                        list = AnimeList.Load(new AniList(act.Credentials));
                        break;
                    default:
                        return null;
                }
                list.SyncStart += OnSyncStart;
                list.SyncStop += OnSyncStop;
                list.SyncError += OnSyncError;
                return list;
            }
            catch(Exception e) {
                var md = new MessageDialog(Win, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.YesNo,
                    "An error was encountered while loading the default anime list: \n" + e.Message +
                    "\n Open the settings window?") { WindowPosition = WindowPosition.Center};
                if(md.Run() == (int)ResponseType.Yes) {
                    md.Destroy();
                    var s = new SettingsWindow(false) { WindowPosition = WindowPosition.Center};
                    if(s.Run() == (int)ResponseType.Accept) {
                        SettingsChanged();
                    }
                    s.Destroy();
                }
                else md.Destroy();
                return null;
            }

        }

        internal static MangaList GetMangaList() {
            var act = Settings.DefaultManga;
            try {
                MangaList list = null;
                switch(act?.Provider) {
                    case "MyAnimeList":
                        list = MangaList.Load(new MyAnimeList(act.Credentials));
                        break;
                    case "Kitsu":
                        list = MangaList.Load(new Kitsu(act.Credentials));
                        break;
                    case "AniList":
                        list = MangaList.Load(new AniList(act.Credentials));
                        break;
                    default:
                        return null;
                }
                list.SyncStart += OnSyncStart;
                list.SyncStop += OnSyncStop;
                list.SyncError += OnSyncError;
                return list;
            }
            catch(Exception e) {
                var md = new MessageDialog(Win, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.YesNo,
                    "An error was encountered while loading the default manga list: \n" + e.Message +
                    "\n Open the settings window?") { WindowPosition = WindowPosition.Center};
                if(md.Run() == (int)ResponseType.Yes) {
                    md.Destroy();
                    var s = new SettingsWindow(false) { WindowPosition = WindowPosition.Center};
                    if(s.Run() == (int)ResponseType.Accept) {
                        SettingsChanged();
                    }
                    s.Destroy();
                }
                else md.Destroy();
                return null;
            }
        }

        private static void OnSyncStart(object o, EventArgs args) {
            Win?._statusbar.Pop(1);
            Win?._statusbar.Push(1, "Syncing...");
        }

        private static void OnSyncStop(object o, EventArgs args) {
            Win?._statusbar.Pop(1);
        }

        private static void OnSyncError(object o, ErrorEventArgs args) {
            Win?._statusbar.Pop(1);
            Win?._statusbar.Push(2, $"Error syncing: {args.GetException().Message}");
            Task.Delay(3500).Wait();
            Win?._statusbar.Pop(2);
        }
        
    }
}
