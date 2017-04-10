using System.Security.Cryptography;
using System.Text;
using Trackr.core;

namespace Trackr.api {
    /// <summary>
    /// A class for securly storing usernames and passwords for various API calls.
    /// </summary>
    public class UserPass {

        /// <summary>
        /// The user's username.
        /// </summary>
        public string Username { get; set; }
        private byte[] _password;

        /// <summary>
        /// The decrypted password cooresponding to the Username.
        /// </summary>
        public string Password {
            get {
                byte[] bytes = ProtectedData.Unprotect(_password, Program.UserSettings.Entropy,
                    DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            set {
                byte[] bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), Program.UserSettings.Entropy,
                    DataProtectionScope.CurrentUser);
                _password = bytes;
            }
        }

        /// <summary>
        /// Returns the username and password in a curl-readable format
        /// </summary>
        public string Credentials => Username + ":" + Password;

        /// <summary>
        /// Create a new username/password combination
        /// </summary>
        /// <param name="user">The user's username</param>
        /// <param name="pass">The user's password.</param>
        public UserPass(string user, string pass){
            Username = user;
            Password = pass;
        }
    }
}