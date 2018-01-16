using System;
using System.Linq;
using Trackr.Api;
using Trackr.Core;
using Trackr.List;

namespace Trackr.Gui.Common {
	/// <summary>
	/// Class containing user information for the running of the program.
	/// </summary>
	public class User {

		public Settings Settings { get; }

		public AnimeList AnimeList { get; set; }

		public MangaList MangaList { get; set; }

		public User() {
			Settings = Trackr.Core.Settings.Load();
			IAnime anime = ResolveAnimeClient(Settings);
			if(anime != null)
				AnimeList.Load(anime);
			IManga manga = ResolveMangaClient(Settings);
			if(manga != null)
				MangaList.Load(manga);
		}

		private static IAnime ResolveAnimeClient(Settings Settings) {
			if(string.IsNullOrEmpty(Settings.DefaultAnime))
				return null;
			string[] animelogin = Settings.DefaultAnime.Split('@');
			if(animelogin.Length != 2)
				return null;
			var passkey = Settings.Accounts.Where(x => x.Key == animelogin[0] && x.Value.Username == animelogin[1]);
			if(!passkey.Any())
				return null;
			var pass = passkey.First();

			switch(pass.Key) {
				case MyAnimeList.Identifier:
					return new MyAnimeList(pass.Value);
				case Kitsu.Identifier:
					return new Kitsu(pass.Value);
				case AniList.Identifier:
					return new AniList(pass.Value.Password, true);
				default:
					return null;
			}
		}

		private static IManga ResolveMangaClient(Settings Settings) {
			if(string.IsNullOrEmpty(Settings.DefaultManga))
				return null;
			string[] mangalogin = Settings.DefaultManga.Split('@');
			if(mangalogin.Length != 2)
				return null;
			var passkey = Settings.Accounts.Where(x => x.Key == mangalogin[0] && x.Value.Username == mangalogin[1]);
			if(!passkey.Any())
				return null;
			var pass = passkey.First();

			switch(pass.Key) {
				case MyAnimeList.Identifier:
					return new MyAnimeList(pass.Value);
				case Kitsu.Identifier:
					return new Kitsu(pass.Value);
				case AniList.Identifier:
					return new AniList(pass.Value.Password, true);
				default:
					return null;
			}
		}
	}
}

