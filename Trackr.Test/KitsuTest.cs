using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
}
}