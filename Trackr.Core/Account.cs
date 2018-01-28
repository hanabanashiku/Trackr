namespace Trackr.Core {
	/// <summary>
	/// An account to be used to connect to an API
	/// </summary>
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
		/// The credentials being used.
		/// </summary>
		/// <remarks>
		/// For OAuth tokens (e.g. AniList), the username will be the 
		/// token type and the password will be the token.
		/// </remarks>
		public UserPass Credentials;

		public Account(string prov, string username, UserPass credentials) {
			Provider = prov;
			Username = username;
			Credentials = credentials;
		}
	}
}