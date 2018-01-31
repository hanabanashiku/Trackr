using Gtk;
using Trackr.List;
using Settings = Trackr.Core.Settings;


namespace Trackr.Gui.Gtk {
    public static class Program {

        public static SystemTray Tray;
        public static MainWindow Win;
        public static Settings Settings; // Reference to Core.Program.UserSettings
        public static AnimeList AnimeList;
        public static MangaList MangaList;
        
        public static void Main(string[] args) {
            // If the settings file doesn't exist, force the user to set up account.
            var force = !Settings.Exists;
            // This will load the settings file for us or return a new one (if force is true)
            //TODO: deal with the exception from Settings
            Core.Program.UserSettings = Settings.Load();
            Settings = Core.Program.UserSettings;
            
            Application.Init();
            if(force) {
                var s = new SettingsWindow(true);
            s.Run();
            s.Destroy();
            }
            Application.Run();
        }
    }
}
