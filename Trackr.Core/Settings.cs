using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;

namespace Trackr.Core {
    /// <summary>
    /// A class for storing user settings
    /// </summary>
    [Serializable]
    public class Settings {
        public byte[] Entropy { get; }
        /// <summary>
        /// The User's encrypted credentials.
        /// </summary>
        public Dictionary<string, UserPass> Credentials { get; }

        private static readonly string Path = System.IO.Path.Combine(
            Program.AppDataPath, "settings.xml");


        private Settings(){
            Entropy = new byte[20];
            using(var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(Entropy);
            Credentials = new Dictionary<string, UserPass>();
        }

        /// <summary>
        /// Load the settings instance
        /// </summary>
        /// <returns>The settings instance from memory, or a new one</returns>
        /// <exception cref="InvalidOperationException">If the given settings file is incompatible.</exception>
        public static Settings Load(){
            FileStream fs = null;
            try {
                var f = new BinaryFormatter();
                fs = new FileStream(Path, FileMode.Open);
                return (Settings) f.Deserialize(fs);
            }
            catch (IOException) {
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
                var f = new BinaryFormatter();
                f.Serialize(fs, this);
            }
        }

        ~Settings(){
            Save();
        }
    }
}