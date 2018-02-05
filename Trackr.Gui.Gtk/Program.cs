using System;
using System.Reflection;
using Gdk;
using Gtk;
using Trackr.Api;
using Trackr.Core;
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
                   
            Application.Init();
            if(force)
                using (var s = new SettingsWindow(true)) {
                    var res = s.Run();
                    s.Destroy();
                    if (res != (int) ResponseType.Accept)
                        Environment.Exit(0);
                }
            // Get our lists ready (any sync them)
            AnimeList = GetAnimeList();
            MangaList = GetMangaList();
            
            Console.WriteLine(Settings.DefaultAnime == null);
            
            // we have a settings file! Spawn our notification icon and window
            _tray = new SystemTray();
            Win = new MainWindow { Visible = Settings.ShowWindowOnStart };
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
        /// <returns></returns>
        // TODO: Handle instantiation errors
        public static AnimeList GetAnimeList() {
            var act = Settings.DefaultAnime;
            switch(act.Provider) {
                    case "MyAnimeList":
                        return AnimeList.Load(new MyAnimeList(act.Credentials));
                    case "Kitsu":
                        return AnimeList.Load(new Kitsu(act.Credentials));
                    case "AniList":
                        throw new NotImplementedException();
                    default:
                        return null;
            }
        }

        public static MangaList GetMangaList() {
            var act = Settings.DefaultManga;
            if(act == null) return null;
            switch(act.Provider) {
                case "MyAnimeList":
                        return MangaList.Load(new MyAnimeList(act.Credentials));
                case "Kitsu":
                        return MangaList.Load(new Kitsu(act.Credentials));
                case "AniList":
                    throw new NotImplementedException();
                 default: 
                     return null;
            }
        }
    }
}
