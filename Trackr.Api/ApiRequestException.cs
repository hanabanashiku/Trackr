using System;

namespace Trackr.Api {
    /// <summary>
    /// An Exception to be thrown when an API request has failed.
    /// </summary>
    public class ApiRequestException : Exception{
        public ApiRequestException(string msg) : base(msg) { }
    }
}