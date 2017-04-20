using System;

namespace Trackr.Api {
    /// <summary>
    /// The API call's response was malformed.
    /// </summary>
    public class ApiFormatException : Exception {
        public ApiFormatException(string msg) : base(msg){}
    }
}