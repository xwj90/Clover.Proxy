
namespace Clover.Proxy
{
    using System;

    /// <summary>
    /// Defines the category items for a <see cref="LogEntry"/> object.
    /// </summary>
    [Serializable]
    public enum LogCategory
    {
        /// <summary>
        /// Indicates that the log entry doesn't belong to a specified category.
        /// </summary>
        None = 0, 

        /// <summary>
        /// Indicates that the data of the specified log entry is informational.
        /// </summary>
        Info = 1, 

        /// <summary>
        /// Indicates that the data of the specified log entry is for debug purpose.
        /// </summary>
        Debug = 2, 

        /// <summary>
        /// Indicates that the data of the specified log entry is for medium level warnings.
        /// </summary>
        Warning = 3, 

        /// <summary>
        /// Indicates that the data of the specified log entry is for critical errors.
        /// </summary>
        Error = 4
    }
}
