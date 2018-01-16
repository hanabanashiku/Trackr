using System;

namespace Trackr.Core {
	/// <summary>
	/// Thrown when the client fails to initialize as expected.
	/// </summary>
	public class InitializationException : Exception {
		public InitializationException(string msg) : base(msg) {
		}
	}
}

