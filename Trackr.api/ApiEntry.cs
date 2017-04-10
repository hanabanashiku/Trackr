using System;

namespace Trackr.api {
    /// <summary>
    /// Represents an item result from an API call
    /// </summary>
    public abstract class ApiEntry {

        /// <summary>
        /// The List Status of the given entry
        /// </summary>
        public enum Status {
            NotInList = 0,
            Current = 1,
            Completed = 2,
            OnHold = 3,
            Dropped = 4,
            Planed = 6
        };
        public string Title;
    }
}