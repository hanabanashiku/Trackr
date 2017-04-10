namespace Trackr.core {
    public class Program {
        public static Settings UserSettings;

        public static void Main(){
            UserSettings = Settings.Load();
        }
    }
}