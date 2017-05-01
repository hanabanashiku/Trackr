using System;
using Trackr.Api;
using Trackr.Core;
using Trackr.List;

namespace Trackr.Test {
    /*
     * This file was created as a fallback due to a bug involving NUnit 2.6.4
     * and IEquatable. I can only assume this was fixed in NUnit3, but none of the IDEs
     * being used work with NUnit3 currently except VS. As such, this class gets around
     * the need to use the framework to test. */
    public class AnimeListFallbackTest {
        public static void Main(){
            Program.Init();
            var mal = new MyAnimeList(new UserPass("trackrtest", "MWhXDyAUQdxa"));
            Assert(mal.VerifyCredentials().Result);
            var list = AnimeList.Load(mal);
            var result = list.Find("Bleach").Result;
            Assert(result.Count > 0);
            list.Add(result[0]);
            Assert(list.Contains(result[0]));
            Assert(list.Sync().Result);
            var pull = mal.PullAnimeList().Result;
            Assert(pull.Contains(result[0]));
            list.Remove(result[0]);
            Assert(!list.Contains(result[0]));
            Assert(list.Sync().Result);
            pull = mal.PullAnimeList().Result;
            Assert(!pull.Contains(result[0]));
            Console.WriteLine("Test pass!");
        }

        private static void Assert(bool result){
            if(!result)
                throw new Exception("Assertion failed");
        }
    }
}