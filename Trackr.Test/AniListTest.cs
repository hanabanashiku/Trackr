using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gtk;
using NUnit.Framework;
using Trackr.Api;
using Trackr.Gui.Gtk;
using Trackr.Core;

namespace Trackr.Test {
	[TestFixture]
	public class AniListTest {
		private AniList _aniList;
		
		[OneTimeSetUp]
		public async Task SetUp() {
			var d = new AniListLogin();
			if(d.Run() != (int)ResponseType.Accept) Assert.Fail();
			var cred = new UserPass(null, d.Pin);
			d.Destroy();
			
			_aniList = new AniList(cred);
			Assert.True(await _aniList.VerifyCredentials());
			Assert.AreEqual(_aniList.Username, "trackrtest"); // Run this with this account
		}
	}
}