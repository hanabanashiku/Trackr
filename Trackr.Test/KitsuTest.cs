using System;
using System.Linq;
using NUnit.Framework;
using Trackr.Api;
using Trackr.Core;

namespace Trackr.Test {
	[TestFixture]
	public class KitsuTest {
		private Kitsu _kitsu;
		private UserPass _credentials;

		[OneTimeSetUp]
		public void SetUp() {
			_credentials = new UserPass("trackrtest", "MWhXDyAUQdxa");
			Assert.AreEqual(_credentials.Username, "trackrtest");
			Assert.AreEqual(_credentials.Password, "MWhXDyAUQdxa");
			_kitsu = new Kitsu(_credentials);
			Assert.True(_kitsu.VerifyCredentials().Result);
		}

		[Test]
		public void AddRemoveAnime() {
			Assert.True(_kitsu.AddAnime(12, ApiEntry.ListStatuses.Current).Result); // One Piece
			Assert.True(_kitsu.AddAnime(1376, ApiEntry.ListStatuses.Completed).Result); // Death Note
			Assert.True(_kitsu.AddAnime(12243, ApiEntry.ListStatuses.Planned).Result); // Kobayashi-san Chi no Maid Dragon
			var anime = _kitsu.PullAnimeList().Result;
			Assert.AreNotEqual(anime.Count, 0);
			Assert.True(anime.Any(x => x.Title == "One Piece"));
			Assert.True(anime.Any(x => x.Title == "Death Note"));
			Assert.True(anime.Any(x => x.Title == "Kobayashi-san Chi no Maid Dragon"));
			Assert.True(_kitsu.RemoveAnime(12).Result);
			Assert.True(_kitsu.RemoveAnime(1376).Result);
			Assert.True(_kitsu.RemoveAnime(12243).Result);
			anime = _kitsu.PullAnimeList().Result;
			Assert.AreEqual(anime.Count, 0);
		}

		[Test]
		public void UpdateAnime() {
			Assert.True(_kitsu.AddAnime(12).Result); // One Piece
			Assert.True(_kitsu.AddAnime(1376).Result); // Death Note
			Assert.True(_kitsu.AddAnime(12243).Result); // Kobayashi-san Chi no Maid Dragon
			Assert.True(_kitsu.AddAnime(11).Result); // Naruto
			var anime = _kitsu.PullAnimeList().Result;
			Assert.AreNotEqual(anime.Count, 0);

			var onepiece = anime.First(x => x.Id == 12);
			onepiece.UserScore = 10;
			onepiece.CurrentEpisode = 718;
			onepiece.UserStart = DateTime.Today;
			Assert.True(_kitsu.UpdateAnime(onepiece).Result);
			
			var deathnote = anime.First(x => x.Id == 1376);
			deathnote.ListStatus = ApiEntry.ListStatuses.Completed;
			Assert.True(_kitsu.UpdateAnime(deathnote).Result);

			var dragonmaid = anime.First(x => x.Id == 12243);
			dragonmaid.ListStatus = ApiEntry.ListStatuses.Completed;
			dragonmaid.UserStart = dragonmaid.StartDate;
			dragonmaid.UserEnd = DateTime.Today;
			dragonmaid.UserScore = 9;
			Assert.True(_kitsu.UpdateAnime(dragonmaid).Result);

			var naruto = anime.First(x => x.Id == 11);
			naruto.ListStatus = ApiEntry.ListStatuses.NotInList;
			Assert.True(_kitsu.UpdateAnime(naruto).Result);

			anime = _kitsu.PullAnimeList().Result;
			
			onepiece = anime.First(x => x.Id == 12);
			Assert.AreEqual(onepiece.UserScore, 10);
			Assert.AreEqual(onepiece.CurrentEpisode, 718);
			Assert.AreEqual(onepiece.UserStart.Date, DateTime.Today.Date);
			
			deathnote = anime.First(x => x.Id == 1376);
			Assert.AreEqual(deathnote.ListStatus, ApiEntry.ListStatuses.Completed);

			dragonmaid = anime.First(x => x.Id == 12243);
			Assert.AreEqual(dragonmaid.ListStatus, ApiEntry.ListStatuses.Completed);
			Assert.AreEqual(dragonmaid.UserStart.Date, dragonmaid.StartDate.Date);
			Assert.AreEqual(dragonmaid.UserEnd.Date, DateTime.Today.Date);
			Assert.AreEqual(dragonmaid.UserScore, 9);
			
			Assert.False(anime.Exists(x => x.Id == 11));
		}
		
		[Test]
		public void FindAnime(){
			var anime = _kitsu.FindAnime("Death Note").Result;
			Assert.True(anime.Exists(x => x.Id == 1376));
			Assert.True(anime.Exists(x => x.Id == 2707));

			anime = _kitsu.FindAnime("Pokémon").Result;
			Assert.True(anime.Any(x => x.Id == 486));
			Assert.True(anime.Any(x => x.Id == 7922));
			Assert.True(anime.Any(x => x.Id == 12531));
		}

		[Test]
		public void AnimeValues(){
			var academia = _kitsu.FindAnime("My Hero Academia").Result.First(x => x.Id == 11469);
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
			Assert.AreEqual(academia.Synopsis, "The appearance of \"quirks,\" newly discovered super powers, has been steadily increasing over the years, with 80 percent of humanity possessing various abilities from manipulation of elements to shapeshifting. This leaves the remainder of the world completely powerless, and Izuku Midoriya is one such individual.\r\n\r\nSince he was a child, the ambitious middle schooler has wanted nothing more than to be a hero. Izuku's unfair fate leaves him admiring heroes and taking notes on them whenever he can. But it seems that his persistence has borne some fruit: Izuku meets the number one hero and his personal idol, All Might. All Might's quirk is a unique ability that can be inherited, and he has chosen Izuku to be his successor!\r\n\r\nEnduring many months of grueling training, Izuku enrolls in U.A. High, a prestigious high school famous for its excellent hero training program, and this year's freshmen look especially promising. With his bizarre but talented classmates and the looming threat of a villainous organization, Izuku will soon learn what it really means to be a hero.\r\n\r\n(Source: MAL)");
		}

		[Test]
		public void AddRemoveManga(){
			Assert.True(_kitsu.AddManga(1984).Result); // Soul Eater
			Assert.True(_kitsu.AddManga(37).Result); // Bleach
			Assert.True(_kitsu.AddManga(6182).Result); // Kaichou wa Maid-sama!
			var manga = _kitsu.PullMangaList().Result;
			Assert.AreNotEqual(manga.Count, 0);
			Assert.True(manga.Any(x => x.Id == 1984));
			Assert.True(manga.Any(x => x.Id == 37));
			Assert.True(manga.Any(x => x.Id == 6182));
		}

		[Test]
		public void UpdateManga(){
			Assert.True(_kitsu.AddManga(1984).Result); // Soul Eater
			Assert.True(_kitsu.AddManga(37).Result); // Bleach
			Assert.True(_kitsu.AddManga(6182).Result); // Kaichou wa Maid-sama!
			Assert.True(_kitsu.AddManga(24266).Result); // Be Free!
			var manga = _kitsu.PullMangaList().Result;
			Assert.AreNotEqual(manga.Count, 0);

			var souleater = manga.First(x => x.Id == 1984);
			souleater.ListStatus = ApiEntry.ListStatuses.Completed;
			souleater.UserStart = souleater.StartDate;
			souleater.UserEnd = DateTime.Today;
			souleater.UserScore = 10;
			Assert.True(_kitsu.UpdateManga(souleater).Result);

			var bleach = manga.First(x => x.Id == 37);
			bleach.ListStatus = ApiEntry.ListStatuses.Planned;
			Assert.True(_kitsu.UpdateManga(bleach).Result);

			var maid = manga.First(x => x.Id == 6182);
			maid.ListStatus = ApiEntry.ListStatuses.Current;
			maid.CurrentChapter = 5;
			maid.UserStart = DateTime.Today;
			maid.UserScore = 9;
			Assert.True(_kitsu.UpdateManga(maid).Result);

			var befree = manga.First(x => x.Id == 24266);
			befree.ListStatus = ApiEntry.ListStatuses.NotInList;
			Assert.True(_kitsu.UpdateManga(befree).Result);

			manga = _kitsu.PullMangaList().Result;
			
			souleater = manga.First(x => x.Id == 1984);
			Assert.AreEqual(souleater.ListStatus, ApiEntry.ListStatuses.Completed);
		    Assert.AreEqual(souleater.UserStart, souleater.StartDate);
			Assert.AreEqual(souleater.UserEnd, DateTime.Today);
			Assert.AreEqual(souleater.UserScore, 10);
			Assert.AreEqual(souleater.CurrentChapter, souleater.Chapters);

			bleach = manga.First(x => x.Id == 37);
			Assert.AreEqual(bleach.ListStatus, ApiEntry.ListStatuses.Planned);
			Assert.AreEqual(bleach.UserStart, DateTime.MinValue);

			maid = manga.First(x => x.Id == 6182);
			Assert.AreEqual(maid.ListStatus, ApiEntry.ListStatuses.Current);
			Assert.AreEqual(maid.CurrentChapter, 5);
			Assert.AreEqual(maid.UserStart, DateTime.Today);
			Assert.AreEqual(maid.UserScore, 9);
			
			Assert.False(manga.Any(x => x.Id == 24266));
		}

		[Test]
		public void FindManga(){
			var manga = _kitsu.FindManga("boku dake ga inai machi").Result;
			Assert.True(manga.Any(x => x.Id == 19100));
			manga = _kitsu.FindManga("Pokémon").Result;
			Assert.True(manga.Any(x => x.Id == 2023));
		}

		[Test]
		public void MangaValues(){ // passes as of 2017
			var yotsuba = _kitsu.FindManga("Yotsuba to!").Result.First(x => x.Id == 272);
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
		public async void TearDown() {
			var anime = _kitsu.PullAnimeList().Result;
			foreach(var a in anime)
				await _kitsu.RemoveAnime(a.Id);
			var manga = _kitsu.PullMangaList().Result;
			foreach(var m in manga)
				await _kitsu.RemoveManga(m.Id);
		}
	}
}