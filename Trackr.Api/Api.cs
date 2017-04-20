using System;

namespace Trackr.Api {
    /// <summary>
    /// A base class from which all API handles are derived.
    /// </summary>
    public abstract class Api {
        /// <summary>
        /// The name of the current API.
        /// </summary>
        public static string Name;

        /// <summary>
        /// The username of the user logged into the API.
        /// </summary>
        public string Username;

        protected static string UserAgent =
            "Trackr/" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() +
            " (" + Environment.OSVersion.Platform.ToString() + ")";

    }
}