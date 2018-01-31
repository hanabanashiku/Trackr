﻿using System;
 using System.Threading.Tasks;

namespace Trackr.Api {
    /// <summary>
    /// A base class from which all API handles are derived.
    /// </summary>
    public abstract class Api {
        /// <summary>
        /// The name of the current API.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The username of the user logged into the API.
        /// </summary>
        public abstract string Username { get; }

        protected static string UserAgent =
            "Trackr/" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() +
            " (" + Environment.OSVersion.Platform.ToString() + ")";

        /// <summary>
        /// Verify the user's API credentials.
        /// </summary>
        /// <returns>true for valid credentials.</returns>
        public abstract Task<bool> VerifyCredentials();

    }
}