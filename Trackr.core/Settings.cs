using System;
using System.Security.Cryptography;

namespace Trackr.core {
    [Serializable]
    public class Settings {
        public byte[] Entropy { get; private set; }

        private Settings(){
            Entropy = new byte[20];
            using(var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(Entropy);
        }

        public static Settings Load(){
            return null;
        }

        public void Save(){

        }

        ~Settings(){
            Save();
        }
    }
}