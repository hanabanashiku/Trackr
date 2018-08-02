using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Trackr.Api;
using Trackr.Core;

namespace Trackr.Test {
	[TestFixture]
	public class AniListTest {
		private AniList _aniList; // cupuvit@send22u.info trackrtest2 MWhXDyAUQdxa
		private const string Username = "trackrtest2";
		
		[OneTimeSetUp]
		public async Task SetUp() {
			Program.UserSettings = Settings.Load();
			
			// Make sure there is an account called trackr2@anilist in settings file
			var act = Program.UserSettings.Accounts.FirstOrDefault(x => x.Provider == "AniList" && x.Username == Username);
			
			Assert.AreNotEqual(act, null);
			
			_aniList = new AniList(act);
			Assert.True(await _aniList.VerifyCredentials());
			Assert.AreEqual(_aniList.Username, Username); // Run this with this account
		}
		
		[Test]
		public void AddRemoveAnime() {
			Assert.True(_aniList.AddAnime(21, ApiEntry.ListStatuses.Current).Result); // One Piece
			Assert.True(_aniList.AddAnime(1535, ApiEntry.ListStatuses.Completed).Result); // Death Note
			Assert.True(_aniList.AddAnime(21776, ApiEntry.ListStatuses.Planned).Result); // Kobayashi-san Chi no Maid Dragon
			var anime = _aniList.PullAnimeList().Result;
			Assert.AreNotEqual(anime.Count, 0);
			Assert.True(anime.Any(x => x.Title == "One Piece"));
			Assert.True(anime.Any(x => x.Title == "Death Note"));
			Assert.True(anime.Any(x => x.Title == "Kobayashi-san Chi no Maidragon"));
			Assert.True(_aniList.RemoveAnime(12).Result);
			Assert.True(_aniList.RemoveAnime(1376).Result);
			Assert.True(_aniList.RemoveAnime(12243).Result);
			anime = _aniList.PullAnimeList().Result;
			Assert.AreEqual(anime.Count, 0);
		}

		[Test] // TODO: Works, but there seems to be some timezone issues going on
		public void UpdateAnime() {
			Assert.True(_aniList.AddAnime(21).Result); // One Piece
			Assert.True(_aniList.AddAnime(1535).Result); // Death Note
			Assert.True(_aniList.AddAnime(21776).Result); // Kobayashi-san Chi no Maid Dragon
			Assert.True(_aniList.AddAnime(20).Result); // Naruto
			var anime = _aniList.PullAnimeList().Result;
			Assert.AreNotEqual(anime.Count, 0);

			var onepiece = anime.First(x => x.Id == 21);
			onepiece.UserScore = 10;
			onepiece.CurrentEpisode = 718;
			onepiece.UserStart = new DateTime(2018, 2, 8);
			Assert.True(_aniList.UpdateAnime(onepiece).Result);
			
			var deathnote = anime.First(x => x.Id == 1535);
			deathnote.ListStatus = ApiEntry.ListStatuses.Completed;
			Assert.True(_aniList.UpdateAnime(deathnote).Result);

			var dragonmaid = anime.First(x => x.Id == 21776);
			dragonmaid.ListStatus = ApiEntry.ListStatuses.Completed;
			dragonmaid.UserStart = dragonmaid.StartDate;
			dragonmaid.UserEnd = DateTime.Today.Date;
			dragonmaid.UserScore = 9;
			Assert.True(_aniList.UpdateAnime(dragonmaid).Result);

			var naruto = anime.First(x => x.Id == 20);
			naruto.ListStatus = ApiEntry.ListStatuses.NotInList;
			Assert.True(_aniList.UpdateAnime(naruto).Result);

			anime = _aniList.PullAnimeList().Result;
			
			onepiece = anime.First(x => x.Id == 21);
			Assert.AreEqual(onepiece.UserScore, 10);
			Assert.AreEqual(onepiece.CurrentEpisode, 718);
			Assert.AreEqual(onepiece.UserStart.Date, new DateTime(2018, 2, 8).Date);
			
			deathnote = anime.First(x => x.Id == 1535);
			Assert.AreEqual(deathnote.ListStatus, ApiEntry.ListStatuses.Completed);

			dragonmaid = anime.First(x => x.Id == 21776);
			Assert.AreEqual(dragonmaid.ListStatus, ApiEntry.ListStatuses.Completed);
			Assert.AreEqual(dragonmaid.UserStart.Date, dragonmaid.StartDate.Date);
			Assert.AreEqual(dragonmaid.UserEnd.Date, DateTime.Today.Date);
			Assert.AreEqual(dragonmaid.UserScore, 9);
			
			Assert.False(anime.Exists(x => x.Id == 20));
		}
		
		[Test]
		public void FindAnime(){
			var anime = _aniList.FindAnime("Death Note").Result;
			Assert.True(anime.Exists(x => x.Id == 1535));
			Assert.True(anime.Exists(x => x.Id == 2994));

			anime = _aniList.FindAnime("Pokémon").Result;
			Assert.True(anime.Any(x => x.Id == 527));
			Assert.True(anime.Any(x => x.Id == 21356));
			Assert.True(anime.Any(x => x.Id == 8438));
		}

		[Test]
		public void AnimeValues(){
			var academia = _aniList.FindAnime("My Hero Academia").Result.First(x => x.Id == 21459);
			Assert.NotNull(academia);
			Assert.AreEqual(academia.Title, "Boku no Hero Academia");
			Assert.AreEqual(academia.EnglishTitle, "My Hero Academia");
		    foreach(var x in academia.Synonyms)
		        Console.WriteLine(x);
			Assert.True(academia.Synonyms.Length == 0);
			Assert.AreEqual(academia.Episodes, 13);
			Assert.AreEqual(academia.Type, Anime.ShowTypes.Tv);
			Assert.AreEqual(academia.Status, Anime.RunningStatuses.Completed);
			Assert.AreEqual(academia.StartDate, new DateTime(2016, 04, 03));
			Assert.AreEqual(academia.EndDate, new DateTime(2016, 06, 26));
			Assert.AreEqual(academia.Synopsis, "What would the world be like if 80 percent of the population manifested extraordinary superpowers called “Quirks” at age four? Heroes and villains would be battling it out everywhere! Becoming a hero would mean learning to use your power, but where would you go to study? U.A. High's Hero Program of course! But what would you do if you were one of the 20 percent who were born Quirkless?\r\nMiddle school student Izuku Midoriya wants to be a hero more than anything, but he hasn't got an ounce of power in him. With no chance of ever getting into the prestigious U.A. High School for budding heroes, his life is looking more and more like a dead end. Then an encounter with All Might, the greatest hero of them all gives him a chance to change his destiny…\r\n(Source: Viz Media)");
		}

		[Test]
		public void AddRemoveManga(){
			Assert.True(_aniList.AddManga(30908).Result); // Soul Eater
			Assert.True(_aniList.AddManga(30012).Result); // Bleach
			Assert.True(_aniList.AddManga(32921).Result); // Kaichou wa Maid-sama!
			var manga = _aniList.PullMangaList().Result;
			Assert.AreNotEqual(manga.Count, 0);
			Assert.True(manga.Any(x => x.Id == 30908));
			Assert.True(manga.Any(x => x.Id == 30012));
			Assert.True(manga.Any(x => x.Id == 32921));
		}

		[Test]
		public void UpdateManga(){
			Assert.True(_aniList.AddManga(30908).Result); // Soul Eater
			Assert.True(_aniList.AddManga(30012).Result); // Bleach
			Assert.True(_aniList.AddManga(32921).Result); // Kaichou wa Maid-sama!
			Assert.True(_aniList.AddManga(43601).Result); // Be Free
			var manga = _aniList.PullMangaList().Result;
			Assert.AreNotEqual(manga.Count, 0);

			var souleater = manga.First(x => x.Id == 30908);
			souleater.ListStatus = ApiEntry.ListStatuses.Completed;
			souleater.UserStart = souleater.StartDate;
			souleater.UserEnd = DateTime.Today;
			souleater.UserScore = 10;
			Assert.True(_aniList.UpdateManga(souleater).Result);

			var bleach = manga.First(x => x.Id == 30012);
			bleach.ListStatus = ApiEntry.ListStatuses.Planned;
			Assert.True(_aniList.UpdateManga(bleach).Result);

			var maid = manga.First(x => x.Id == 32921);
			maid.ListStatus = ApiEntry.ListStatuses.Current;
			maid.CurrentChapter = 5;
			maid.UserStart = DateTime.Today;
			maid.UserScore = 9;
			Assert.True(_aniList.UpdateManga(maid).Result);

			var befree = manga.First(x => x.Id == 43601);
			befree.ListStatus = ApiEntry.ListStatuses.NotInList;
			Assert.True(_aniList.UpdateManga(befree).Result);

			manga = _aniList.PullMangaList().Result;
			
			souleater = manga.First(x => x.Id == 30908);
			Assert.AreEqual(souleater.ListStatus, ApiEntry.ListStatuses.Completed);
		    Assert.AreEqual(souleater.UserStart, souleater.StartDate);
			Assert.AreEqual(souleater.UserEnd, DateTime.Today);
			Assert.AreEqual(souleater.UserScore, 10);
			Assert.AreEqual(souleater.CurrentChapter, souleater.Chapters);

			bleach = manga.First(x => x.Id == 30012);
			Assert.AreEqual(bleach.ListStatus, ApiEntry.ListStatuses.Planned);
			Assert.AreEqual(bleach.UserStart, DateTime.MinValue);

			maid = manga.First(x => x.Id == 32921);
			Assert.AreEqual(maid.ListStatus, ApiEntry.ListStatuses.Current);
			Assert.AreEqual(maid.CurrentChapter, 5);
			Assert.AreEqual(maid.UserStart, DateTime.Today);
			Assert.AreEqual(maid.UserScore, 9);
			
			Assert.False(manga.Any(x => x.Id == 43601));
		}

		[Test]
		public void FindManga(){
			var manga = _aniList.FindManga("boku dake ga inai machi").Result;
			Assert.True(manga.Any(x => x.Id == 69325));
			manga = _aniList.FindManga("Pokémon").Result;
			Assert.True(manga.Any(x => x.Id == 37570));
		}

		[Test]
		public void MangaValues(){ // passes as of 2017
			var yotsuba = _aniList.FindManga("Yotsuba to!").Result.First(x => x.Id == 30104);
			Assert.AreEqual(yotsuba.Title, "Yotsubato!");
			Assert.AreEqual(yotsuba.EnglishTitle, "Yotsuba\u0026!");
			Assert.AreEqual(yotsuba.Synonyms.Length, 0);
			Assert.AreEqual(yotsuba.Chapters, 0);
			Assert.AreEqual(yotsuba.Volumes, 0);
			Assert.AreEqual(yotsuba.Type, Manga.MangaTypes.Manga);
			Assert.AreEqual(yotsuba.Status, Manga.RunningStatuses.Publishing);
			Assert.AreEqual(yotsuba.StartDate, new DateTime(2003, 03, 21));
		}
		
		[TearDown]
		public async Task TearDown(){
			var anime = _aniList.PullAnimeList().Result;
			foreach(var a in anime)
				await _aniList.RemoveAnime(a.Id);
			var manga = _aniList.PullMangaList().Result;
			foreach(var m in manga)
				await _aniList.RemoveManga(m.Id);
		}
	}
}