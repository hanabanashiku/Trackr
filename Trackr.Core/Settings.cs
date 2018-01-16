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
		private static readonly string Path = System.IO.Path.Combine(
			                                      Program.AppDataPath, "settings");

		public byte[] Entropy { get; }

		/// <summary>
		/// Whether or not the settings file exists
		/// </summary>
		public static bool Exists => File.Exists(Path);
		
		/// <summary>
		/// List of all accounts.
		/// </summary>
		/// <remarks>
		/// For OAuth tokens (e.g. Kitsu), the username will be the 
		/// token type and the password will be the token.
		/// </remarks>
		public List<KeyValuePair<string, UserPass>> Accounts { get; }

		/// <summary>
		/// The default anime list account, in format Username@APIName
		/// </summary>
		public string DefaultAnime { get; set; }

		/// <summary>
		/// The default manga list adccount, in format Username@APIName
		/// </summary>
		public string DefaultManga { get; set; }

		/// <summary>
		/// If enabled, the main window will be on top of all other windows.
		/// </summary>
		public bool KeepWindowOnTop { get; set; }

		private Settings() {
			Entropy = new byte[20];
			using(var rng = new RNGCryptoServiceProvider())
				rng.GetBytes(Entropy);
			Accounts = new List<KeyValuePair<string, UserPass>>();
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
				return (Settings)f.Deserialize(fs);
			} catch(IOException) {
				return new Settings();
			} finally {
				fs?.Close();
			}
		}

		/// <summary>
		/// Save the settings file to the hard disk.
		/// </summary>
		public void Save(){
			using(var fs = File.Open(Path, FileMode.Create)) {
				var f = new BinaryFormatter();
				f.Serialize(fs, this);
			}
		}

		~Settings() {
			Save();
		}
	}
}