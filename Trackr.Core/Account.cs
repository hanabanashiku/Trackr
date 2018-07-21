using System;

namespace Trackr.Core {
	/// <summary>
	/// An account to be used to connect to an API
	/// </summary>
	[Serializable]
	public class Account {
		/// <summary>
		/// The API Provider name.
		/// </summary>
		public string Provider;
		/// <summary>
		/// The username being used.
		/// </summary>
		public string Username;
		/// <summary>
		/// The account email
		/// </summary>
		/// <remarks>
		/// Pretty much just for Kitsu, as you need the email (not the username) to log in.
		/// </remarks>
		public string Email;
		/// <summary>
		/// The credentials being used.
		/// </summary>
		/// <remarks>
		/// For OAuth tokens (e.g. AniList), the password will be the refresh token.
		/// </remarks>
		public UserPass Credentials;


		/// <param name="prov">The API provider</param>
		/// <param name="username">The username of the account</param>
		/// <param name="credentials">The account credentials</param>
		/// <param name="email">The account email address</param>
		public Account(string prov, string username, UserPass credentials, string email = "") {
			Provider = prov;
			Username = username;
			Credentials = credentials;
			Email = email;
		}

		// Copy constructor
		public Account(Account a) {
			Provider = a.Provider;
			Username = a.Username;
			Credentials = new UserPass(a.Credentials.Username, a.Credentials.Password);
			Email = a.Email;
		}

		public static bool operator ==(Account a, Account b) {
			if(ReferenceEquals(null, a) && ReferenceEquals(null, b)) return true;
			if(ReferenceEquals(null, a) || ReferenceEquals(null, b)) return false;
			return a.Provider == b.Provider && a.Username == b.Username;
		}

		public static bool operator !=(Account a, Account b) { return !(a == b); }

		protected bool Equals(Account a) {
			return this == a;
		}
		
		public override bool Equals(object obj) {
			if(ReferenceEquals(null, obj)) return false;
			if(ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((Account) obj);
		}
		
		public override int GetHashCode() {
			unchecked {
				// ReSharper disable NonReadonlyMemberInGetHashCode
				var hashCode = (Provider != null ? Provider.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Username != null ? Username.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Credentials != null ? Credentials.GetHashCode() : 0);
				return hashCode;
			}
		}
	}
}