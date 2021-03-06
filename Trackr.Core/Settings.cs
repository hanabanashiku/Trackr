﻿using System;
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
		private static readonly string FileName = Path.Combine(
			                                      Program.AppDataPath, "settings");

		public byte[] Entropy { get; }

		public enum TitleDisplays {Romaji, Japanese, English }

		/// <summary>
		/// Whether or not the settings file exists
		/// </summary>
		public static bool Exists => File.Exists(FileName);
		
		/// <summary>
		/// List of all accounts.
		/// </summary>
		public List<Account> Accounts { get; }

		/// <summary>
		/// The default anime list account
		/// </summary>
		public Account DefaultAnime { get; set; }

		/// <summary>
		/// The default manga list account
		/// </summary>
		public Account DefaultManga { get; set; }

		/// <summary>
		/// If enabled, the main window will be on top of all other windows.
		/// </summary>
		public bool KeepWindowOnTop { get; set; }
		
		/// <summary>
		/// If enabled, the main window will be shown at startup.
		/// </summary>
		public bool ShowWindowOnStart { get; set; }
		
		/// <summary>
		/// If enabled, the main window will close to system tray instead of closing the app.
		/// </summary>
		public bool MinimizeToTray { get; set; }
		
		/// <summary>
		/// Set the method for displaying titles (default is romaji).
		/// </summary>
		public TitleDisplays TitleDisplay { get; set; }

		private Settings() {
			Entropy = new byte[20];
			using(var rng = new RNGCryptoServiceProvider())
				rng.GetBytes(Entropy);
			Accounts = new List<Account>();

			KeepWindowOnTop = false;
			ShowWindowOnStart = true;
			MinimizeToTray = true;
			
			TitleDisplay = TitleDisplays.Romaji;
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
				fs = new FileStream(FileName, FileMode.Open);
				return (Settings)f.Deserialize(fs);
			} catch(Exception e) {
				return new Settings();
			} finally {
				fs?.Close();
			}
		}

		/// <summary>
		/// Save the settings file to the hard disk.
		/// </summary>
		public void Save() {
			// create the directory
			var dir = Path.GetDirectoryName(FileName);
			if(!Directory.Exists(dir) && dir != null)
				Directory.CreateDirectory(dir);
			
			using(var fs = File.Open(FileName, FileMode.Create)) {
				var f = new BinaryFormatter();
				f.Serialize(fs, this);
			}
		}

		/// <summary>
		/// Delete the settings file
		/// </summary>
		public static void Delete() {
			if(File.Exists(FileName))
				File.Delete(FileName);
		}

		~Settings() {
			Save();
		}
	}
}