using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Trackr.Api;
using Trackr.Core;

namespace Trackr.Test {
	[TestFixture]
	public class MyAnimeListTests {
		private MyAnimeList _mal;
		private UserPass _credentials;

	    [OneTimeSetUp]
	    public void SetUp(){
	        Program.Init();
			_credentials = new UserPass("trackrtest", "MWhXDyAUQdxa");
	        Assert.AreEqual(_credentials.Username, "trackrtest");
	        Assert.AreEqual(_credentials.Password, "MWhXDyAUQdxa");
			_mal = new MyAnimeList(_credentials);
			Assert.True(_mal.VerifyCredentials().Result);
		}

		[Test]
		public void AddRemoveAnime(){
			Assert.True(_mal.AddAnime(21).Result); // One Piece
			Assert.True(_mal.AddAnime(1535).Result); // Death Note
			Assert.True(_mal.AddAnime(33206).Result); // Kobayashi-san Chi no Maid Dragon
			var anime = _mal.PullAnimeList().Result;
			Assert.AreNotEqual(anime.Count, 0);
			Assert.True(anime.Any(x => x.Title == "One Piece"));
			Assert.True(anime.Any(x => x.Title == "Death Note"));
			Assert.True(anime.Any(x => x.Title == "Kobayashi-san Chi no Maid Dragon"));
			Assert.True(_mal.RemoveAnime(21).Result);
			Assert.True(_mal.RemoveAnime(1535).Result);
			Assert.True(_mal.RemoveAnime(33206).Result);
			anime = _mal.PullAnimeList().Result;
			Assert.AreEqual(anime.Count, 0);
		}

		[Test]
		public void UpdateAnime(){
			Assert.True(_mal.AddAnime(21).Result); // One Piece
			Assert.True(_mal.AddAnime(1535).Result); // Death Note
			Assert.True(_mal.AddAnime(33206).Result); // Kobayashi-san Chi no Maid Dragon
			Assert.True(_mal.AddAnime(20).Result); // Naruto
			List<Anime> anime = _mal.PullAnimeList().Result;
			Assert.AreNotEqual(anime.Count, 0);

			Anime onepiece = anime.First(x => x.Id == 21);
			onepiece.UserScore = 10;
			onepiece.CurrentEpisode = 718;
			onepiece.UserStart = DateTime.Today;
			Assert.True(_mal.UpdateAnime(onepiece).Result);

			Anime deathnote = anime.First(x => x.Id == 1535);
			deathnote.ListStatus = ApiEntry.ListStatuses.Planned;
			Assert.True(_mal.UpdateAnime(deathnote).Result);

			Anime dragonmaid = anime.First(x => x.Id == 33206);
			dragonmaid.ListStatus = ApiEntry.ListStatuses.Completed;
			dragonmaid.UserStart = dragonmaid.StartDate;
			dragonmaid.UserEnd = DateTime.Today;
			dragonmaid.UserScore = 9;
			Assert.True(_mal.UpdateAnime(dragonmaid).Result);

			Anime naruto = anime.First(x => x.Id == 20);
			naruto.ListStatus = ApiEntry.ListStatuses.NotInList;
			Assert.True(_mal.UpdateAnime(naruto).Result);

			anime = _mal.PullAnimeList().Result;
			onepiece = anime.First(x => x.Id == 21);
			Assert.AreEqual(onepiece.ListStatus, ApiEntry.ListStatuses.Current);
			Assert.AreEqual(onepiece.UserScore, 10);
			Assert.AreEqual(onepiece.UserStart, DateTime.Today);
			Assert.AreEqual(onepiece.CurrentEpisode, 718);

			deathnote = anime.First(x => x.Id == 1535);
			Assert.AreEqual(deathnote.ListStatus, ApiEntry.ListStatuses.Planned);
			Assert.AreEqual(deathnote.UserStart, DateTime.MinValue);

			dragonmaid = anime.First(x => x.Id == 33206);
			Assert.AreEqual(dragonmaid.ListStatus, ApiEntry.ListStatuses.Completed);
			Assert.AreEqual(dragonmaid.UserStart, dragonmaid.StartDate);
			Assert.AreEqual(dragonmaid.UserEnd, DateTime.Today);
			Assert.AreEqual(dragonmaid.UserScore, 9);
			Assert.AreEqual(dragonmaid.CurrentEpisode, dragonmaid.Episodes);

			Assert.False(anime.Exists(x => x.Id == 20));
		}

		[Test]
		public void FindAnime(){
			List<Anime> anime = _mal.FindAnime("Death Note").Result;
			Assert.True(anime.Any(x => x.Id == 1535));
			Assert.True(anime.Any(x => x.Id == 2994));

			anime = _mal.FindAnime("Pokémon").Result;
			Assert.True(anime.Any(x => x.Id == 527));
			Assert.True(anime.Any(x => x.Id == 2363));
			Assert.True(anime.Any(x => x.Id == 20159));
		}

		[Test]
		public void AnimeValues(){
			Anime academia = _mal.FindAnime("My Hero Academia").Result.First(x => x.Id == 31964);
			Assert.NotNull(academia);
			Assert.AreEqual(academia.Title, "Boku no Hero Academia");
			Assert.AreEqual(academia.EnglishTitle, "My Hero Academia");
		    foreach(string x in academia.Synonyms)
		        Console.WriteLine(x);
			Assert.True(academia.Synonyms.Length == 0);
			Assert.AreEqual(academia.Episodes, 13);
			Assert.AreEqual(academia.Type, Anime.ShowTypes.Tv);
			Assert.AreEqual(academia.Status, Anime.RunningStatuses.Completed);
			Assert.AreEqual(academia.StartDate, new DateTime(2016, 04, 03));
			Assert.AreEqual(academia.EndDate, new DateTime(2016, 06, 26));
			Assert.AreEqual(academia.Synopsis, "The appearance of &quot;quirks,&quot; newly discovered super powers, has been steadily increasing over the years, with 80 percent of humanity possessing various abilities from manipulation of elements to shapeshifting. This leaves the remainder of the world completely powerless, and Izuku Midoriya is one such individual.<br />\r\n<br />\r\nSince he was a child, the ambitious middle schooler has wanted nothing more than to be a hero. Izuku&#039;s unfair fate leaves him admiring heroes and taking notes on them whenever he can. But it seems that his persistence has borne some fruit: Izuku meets the number one hero and his personal idol, All Might. All Might&#039;s quirk is a unique ability that can be inherited, and he has chosen Izuku to be his successor!<br />\r\n<br />\r\nEnduring many months of grueling training, Izuku enrolls in UA High, a prestigious high school famous for its excellent hero training program, and this year&#039;s freshmen look especially promising. With his bizarre but talented classmates and the looming threat of a villainous organization, Izuku will soon learn what it really means to be a hero.<br />\r\n<br />\r\n[Written by MAL Rewrite]");
		}

		[Test]
		public void AddRemoveManga(){
			Assert.True(_mal.AddManga(908).Result); // Soul Eater
			Assert.True(_mal.AddManga(12).Result); // Bleach
			Assert.True(_mal.AddManga(2921).Result); // Kaichou wa Maid-sama!
			List<Manga> manga = _mal.PullMangaList().Result;
			Assert.AreNotEqual(manga.Count, 0);
			Assert.True(manga.Any(x => x.Id == 908));
			Assert.True(manga.Any(x => x.Id == 12));
			Assert.True(manga.Any(x => x.Id == 2921));
		}

		[Test]
		public void UpdateManga(){
			Assert.True(_mal.AddManga(908).Result); // Soul Eater
			Assert.True(_mal.AddManga(12).Result); // Bleach
			Assert.True(_mal.AddManga(2921).Result); // Kaichou wa Maid-sama!
			Assert.True(_mal.AddManga(13601).Result); // Be Free!
			List<Manga> manga = _mal.PullMangaList().Result;
			Assert.AreNotEqual(manga.Count, 0);

			Manga souleater = manga.First(x => x.Id == 908);
			souleater.ListStatus = ApiEntry.ListStatuses.Completed;
			souleater.UserStart = souleater.StartDate;
			souleater.UserEnd = DateTime.Today;
			souleater.UserScore = 10;
			Assert.True(_mal.UpdateManga(souleater).Result);

			Manga bleach = manga.First(x => x.Id == 12);
			bleach.ListStatus = ApiEntry.ListStatuses.Planned;
			Assert.True(_mal.UpdateManga(bleach).Result);

			Manga maid = manga.First(x => x.Id == 2921);
			maid.ListStatus = ApiEntry.ListStatuses.Current;
			maid.CurrentChapter = 5;
			maid.UserStart = DateTime.Today;
			maid.UserScore = 9;
			Assert.True(_mal.UpdateManga(maid).Result);

			Manga befree = manga.First(x => x.Id == 13601);
			befree.ListStatus = ApiEntry.ListStatuses.NotInList;
			Assert.True(_mal.UpdateManga(befree).Result);

			manga = _mal.PullMangaList().Result;
			souleater = manga.First(x => x.Id == 908);
			Assert.AreEqual(souleater.ListStatus, ApiEntry.ListStatuses.Completed);
		    Assert.AreEqual(souleater.UserStart, souleater.StartDate);
			Assert.AreEqual(souleater.UserEnd, DateTime.Today);
			Assert.AreEqual(souleater.UserScore, 10);
			Assert.AreEqual(souleater.CurrentChapter, souleater.Chapters);

			bleach = manga.First(x => x.Id == 12);
			Assert.AreEqual(bleach.ListStatus, ApiEntry.ListStatuses.Planned);
			Assert.AreEqual(bleach.UserStart, DateTime.MinValue);

			maid = manga.First(x => x.Id == 2921);
			Assert.AreEqual(maid.ListStatus, ApiEntry.ListStatuses.Current);
			Assert.AreEqual(maid.CurrentChapter, 5);
			Assert.AreEqual(maid.UserStart, DateTime.Today);
			Assert.AreEqual(maid.UserScore, 9);

			Assert.False(manga.Any(x => x.Id == 13601));
		}

		[Test]
		public void FindManga(){
			List<Manga> manga = _mal.FindManga("boku dake ga inai machi").Result;
			Assert.True(manga.Any(x => x.Id == 39325));
			manga = _mal.FindManga("Pokémon").Result;
			Assert.True(manga.Any(x => x.Id == 928));
		}

		[Test]
		public void MangaValues(){ // passes as of 2017
			Manga yotsuba = _mal.FindManga("Yotsuba to!").Result.First(x => x.Id == 104);
			Assert.AreEqual(yotsuba.Title, "Yotsuba to!");
			Assert.AreEqual(yotsuba.EnglishTitle, "Yotsuba&!");
			Assert.AreEqual(yotsuba.Synonyms, new [] { "Yotsuba and!" });
			Assert.AreEqual(yotsuba.Chapters, 0);
			Assert.AreEqual(yotsuba.Volumes, 0);
			Assert.AreEqual(yotsuba.Type, Manga.MangaTypes.Manga);
			Assert.AreEqual(yotsuba.Status, Manga.RunningStatuses.Publishing);
			Assert.AreEqual(yotsuba.StartDate, new DateTime(2013, 03, 21));
		}

		[TearDown]
		public async Task TearDown(){
			var anime = _mal.PullAnimeList().Result;
			foreach(var a in anime)
				await _mal.RemoveAnime(a.Id);
			var manga = _mal.PullMangaList().Result;
			foreach(var m in manga)
				await _mal.RemoveManga(m.Id);
		}
	}
}