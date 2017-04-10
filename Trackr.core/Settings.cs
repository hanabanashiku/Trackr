using System;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace Trackr.core {
    [Serializable]
    public class Settings {
        public byte[] Entropy { get; }

        private static readonly string Path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "trackr", "settings.xml");


        private Settings(){
            Entropy = new byte[20];
            using(var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(Entropy);
        }

        /// <summary>
        /// Load the settings instance
        /// </summary>
        /// <returns>The settings instance from memory, or a new one</returns>
        /// <exception cref="InvalidOperationException">If the given settings file is incompatible.</exception>
        public static Settings Load(){
            FileStream fs = null;
            try {
                fs = File.Open(Path, FileMode.Create);
                var s = new XmlSerializer(typeof(Settings));
                return (Settings) s.Deserialize(fs);
            }
            catch (FileNotFoundException) {
                return new Settings();
            }
            finally {
                fs?.Close();
            }
        }

        /// <summary>
        /// Save the settings file to the hard disk.
        /// </summary>
        public void Save(){
            using (var fs = File.Open(Path, FileMode.Create)) {
                var s = new XmlSerializer(GetType());
                s.Serialize(fs, this);
            }
        }

        ~Settings(){
            Save();
        }
    }
}