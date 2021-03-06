﻿using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Trackr.Api;
using Trackr.Core;
using Trackr.List;

namespace Trackr.Test {
	[TestFixture]
	public class AnimeListTest {
		private MyAnimeList _mal;
		private AnimeList _list;

		[OneTimeSetUp]
		public void SetUpFirst(){
			Program.Init();
			_mal = new MyAnimeList(new UserPass("trackrtest", "MWhXDyAUQdxa"));
			Assert.True(_mal.VerifyCredentials().Result);
			_list = AnimeList.Load(_mal);
			TearDown();
		}

		[SetUp]
		public void SetUp(){
			_list.Sync().Wait();
			Assert.AreEqual(_list.Count(), 0);
		}

		[TearDown]
		public async Task TearDown(){
			var pull = _mal.PullAnimeList().Result;
			foreach(var a in pull)
				await _mal.RemoveAnime(a.Id);
		}

		[Test]
		public void AddRemoveFindTest(){
			var result = _list.Find("Bleach").Result;
			Assert.True(result.Count > 0);
			_list.Add(result[0]);
			Assert.True(_list.Contains(result[0]));
			Assert.True(_list.Sync().Result);
			var pull = _mal.PullAnimeList().Result;
			Assert.True(pull.Contains(result[0]));
			_list.Remove(result[0]);
			Assert.False(_list.Contains(result[0]));
			Assert.True(_list.Sync().Result);
			pull = _mal.PullAnimeList().Result;
			Assert.False(pull.Contains(result[0]));
		}
		
		[Test]
		public void FindAnime(){
			List<Anime> anime = _list.Find("Death Note").Result;
			Assert.True(anime.Any(x => x.Id == 1535));
			Assert.NotNull(anime.First(x => x.Id == 1535).Title);
			Assert.True(anime.Any(x => x.Id == 2994));

			anime = _mal.FindAnime("Pokémon").Result;
			Assert.True(anime.Any(x => x.Id == 527));
			Assert.True(anime.Any(x => x.Id == 2363));
			Assert.True(anime.Any(x => x.Id == 20159));

			anime = _mal.FindAnime("My Hero Academia").Result;
			foreach(var a in anime) {
				Assert.NotNull(a);
				Assert.NotNull(a.Id);
				Assert.NotNull(a.Title);
			}
		}

		[Test]
		public void IndexerTest(){
			var result = _list.Find("Monogatari").Result;
			Assert.True(_list.Count() > 2);
			result[0].ListStatus = ApiEntry.ListStatuses.Completed;
			_list.Add(result[0]);
			result[1].ListStatus = ApiEntry.ListStatuses.Current;
			_list.Add(result[1]);
			result[2].ListStatus = ApiEntry.ListStatuses.Dropped;
			_list.Add(result[2]);
			Assert.True(_list[ApiEntry.ListStatuses.Completed].Contains(result[0]));
			Assert.True(_list[ApiEntry.ListStatuses.Current].Contains(result[1]));
			Assert.True(_list[ApiEntry.ListStatuses.Dropped].Contains(result[2]));
			Assert.NotNull(_list[result[0].Id]);
			Assert.NotNull(_list[result[1].Id]);
			Assert.NotNull(_list[result[2].Id]);
		}
	}
}