using System;
using Gtk;
using Trackr.List;

//using Trackr.Core;

namespace Trackr.Gui.Gtk {
    public static class Program {

        public static Core.Settings Settings;
        public static AnimeList AnimeList;
        public static MangaList MangaList;
        
        public static void Main(string[] args) {
            /*try {
                if(!Core.Settings.Exists) throw new Exception();
                Settings = Core.Settings.Load();
            }
            catch(Exception e) {
                
            }*/
            Application.Init();
            var s = new SettingsWindow(true);
            Application.Run();
        }
    }
}
