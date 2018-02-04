﻿using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Trackr.Core {
    /// <summary>
    /// A class for securly storing usernames and passwords for various API calls.
    /// </summary>
    [Serializable]
    public class UserPass {

        /// <summary>
        /// The user's username.
        /// </summary>
        public string Username { get; set; }
        private string _password;

        /// <summary>
        /// The decrypted password cooresponding to the Username.
        /// </summary>
        // http://stackoverflow.com/questions/10168240/encrypting-decrypting-a-string-in-c-sharp/10177020#10177020
        // Initially used ProtectData but it consistantly threw crypto exception for no apparent reason on mono
        public string Password {
            get {
                string phrase = Convert.ToBase64String(Program.UserSettings.Entropy);
                byte[] bytes = Convert.FromBase64String(_password);
                byte[] salt = bytes.Take(256 / 8).ToArray();
                byte[] iv = bytes.Skip(256 / 8).Take(256 / 8).ToArray();
                byte[] cipher = bytes.Skip(256 / 8 * 2).Take(bytes.Length - (256 / 8 * 2)).ToArray();

                using(var pw = new Rfc2898DeriveBytes(phrase, salt)) {
                    byte[] keybytes = pw.GetBytes(256 / 8);
                    using(var key = new RijndaelManaged()) {
                        key.BlockSize = 256;
                        key.Mode = CipherMode.CBC;
                        key.Padding = PaddingMode.PKCS7;
                        using(var decryptor = key.CreateDecryptor(keybytes, iv)) {
                            using(var ms = new MemoryStream(cipher)) {
                                using(var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read)) {
                                    var clear = new byte[cipher.Length];
                                    var bytecount = cs.Read(clear, 0, clear.Length);
                                    cs.Close();
                                    ms.Close();
                                    return Encoding.UTF8.GetString(clear, 0, bytecount);
                                }
                            }
                        }
                    }
                }
            }
            set {
                string phrase = Convert.ToBase64String(Program.UserSettings.Entropy);
                byte[] salt = Entropy();
                byte[] iv = Entropy();
                byte[] bytes = Encoding.UTF8.GetBytes(value);

                using(var pw = new Rfc2898DeriveBytes(phrase, salt, 1000)) {
                    byte[] keybytes = pw.GetBytes(256 / 8);
                    using(var key = new RijndaelManaged()) {
                        key.BlockSize = 256;
                        key.Mode = CipherMode.CBC;
                        key.Padding = PaddingMode.PKCS7;
                        using(var encryptor = key.CreateEncryptor(keybytes, iv)) {
                            using(var ms = new MemoryStream()) {
                                using(var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)) {
                                    cs.Write(bytes, 0, bytes.Length);
                                    cs.FlushFinalBlock();
                                    byte[] cipher = salt.Concat(iv).Concat(ms.ToArray()).ToArray();
                                    cs.Close();
                                    ms.Close();
                                    _password = Convert.ToBase64String(cipher);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the username and password in a curl-readable format
        /// </summary>
        public string Credentials => Convert.ToBase64String(Encoding.UTF8.GetBytes(Username + ":" + Password));

        /// <summary>
        /// Create a new username/password combination
        /// </summary>
        /// <param name="user">The user's username</param>
        /// <param name="pass">The user's password.</param>
        public UserPass(string user, string pass){
            Username = user;
            Password = pass;
        }

        private static byte[] Entropy(){
            byte[] bytes = new byte[32]; // 256 bits
            using(var rng = new RNGCryptoServiceProvider()) {
                rng.GetBytes(bytes);
            }
            return bytes;
        }
    }
}