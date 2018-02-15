using System;
using System.IO;

namespace Trackr.Core {
    public static class Program {
        public static Settings UserSettings;
        /// <summary>
        /// The storage location for application data
        /// </summary>
        public static readonly string AppDataPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "trackr");
 
        /// <summary>
        /// Initialize the program
        /// </summary>
        public static void Init(){
            if(UserSettings == null)
                UserSettings = Settings.Load();
        }
        
    }
}