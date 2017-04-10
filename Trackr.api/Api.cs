using System;

namespace Trackr.api {
    /// <summary>
    /// A base class from which all API handles are derived.
    /// </summary>
    public abstract class Api {
        public static string Name;

        protected static string UserAgent =
            "Trackr/" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() +
            " (" + Environment.OSVersion.Platform.ToString() + ")";

    }
}