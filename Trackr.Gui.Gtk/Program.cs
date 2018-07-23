using System;
using System.Diagnostics;
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
                AnimeList = null; // The account is null!
            // The list is null (but the default is not.. adding account) OR the account has changed
            else if(AnimeList == null || Settings.DefaultAnime.Provider != AnimeList.Api || Settings.DefaultAnime.Username != AnimeList.Username)
                AnimeList = GetAnimeList();
            if(Settings.DefaultManga == null)
                MangaList = null;
            else if(MangaList == null || Settings.DefaultManga.Provider != MangaList.Api || Settings.DefaultManga.Username != MangaList.Username)
                MangaList = GetMangaList();
            
            // Reload the list - what if the user changed the title type?
            Win.Fill();
        }

        /// <summary>
        /// Select the correct title based on the user's settings
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string GetTitle(ApiEntry e) {
            try {
                switch(Settings.TitleDisplay) {
                    case Settings.TitleDisplays.English:
                        return e.EnglishTitle;
                    case Settings.TitleDisplays.Japanese:
                        return e.JapaneseTitle;
                    default:
                        return e.Title;
                }
            }
            catch(NullReferenceException) {
                if(e?.Title == null)
                    throw;
                return e.Title;
            }
        }

        /// <summary>
        /// Return the correct anime list based on the default value.
        /// </summary>
        /// <returns>The retrieved AnimeList</returns>
        private static AnimeList GetAnimeList() {
            var act = Settings.DefaultAnime;
            try {
                AnimeList list;
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
                return list;
            }
            catch(Exception e) {
                Debug.WriteLine(e.InnerException?.StackTrace ?? e.StackTrace);
                var md = new MessageDialog(Win, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.YesNo,
                    "An error was encountered while loading the default anime list: \n" + (e.InnerException?.Message ?? e.Message) +
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

        private static MangaList GetMangaList() {
            var act = Settings.DefaultManga;
            try {
                MangaList list;
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
                return list;
            }
            catch(Exception e) {
                var md = new MessageDialog(Win, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.YesNo,
                    "An error was encountered while loading the default manga list: \n" + (e.InnerException?.Message ?? e.Message) +
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
        
    }
}
