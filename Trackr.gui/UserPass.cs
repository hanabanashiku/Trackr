using System.Security;
using System.Security.Cryptography;
using System.Text;
using Trackr.core;

namespace Trackr.gui {
    public class UserPass {

        public string Username { get; set; }
        private byte[] _password;

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

        public UserPass(string user, SecureString pass){
            Username = user;

        }
    }
}