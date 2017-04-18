using NUnit.Framework;
using Trackr.api;
namespace Trackr.Test {
    [TestFixture]
    public class MyAnimeListTests {
        private MyAnimeList _mal;
        private UserPass _credentials;

        [SetUp]
        protected void SetUp(){
            _credentials = new UserPass("trackrtest", "MWhXDyAUQdxa");
            _mal = new MyAnimeList(_credentials);
            Assert.True(_mal.VerifyCredentials().Result);
        }
    }
}