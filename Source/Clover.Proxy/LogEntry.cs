
namespace Clover.Proxy
{
    #region Using directives

    using System;

    #endregion

    /// <summary>
    /// Represents a log entry to write to the service log. 
    /// </summary>
    [Serializable]
    public class LogEntry
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> class by specifying the category and the detailed information to write.
        /// </summary>
        /// <param name="category">
        /// The log category of this instance.
        /// </param>
        /// <param name="data">
        /// The data containing the detailed information to write to log.
        /// </param>
        public LogEntry(LogCategory category, object data)
        {
            this.Id = Guid.NewGuid();
            this.Category = category;
            this.Created = DateTime.Now;
            this.Data = data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> class by specifying the detailed information to write.
        /// </summary>
        /// <param name="data">
        /// The data containing detailed information to write to log.
        /// </param>
        public LogEntry(object data)
            : this(LogCategory.None, data)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> class.
        /// </summary>
        public LogEntry()
            : this(LogCategory.None, null)
        { 
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the category of this instance.
        /// </summary>
        /// <value>
        /// <see cref="LogCategory"/> that represents the category of this instance.
        /// </value>
        public LogCategory Category { get; set; }

        /// <summary>
        /// Gets or sets the created time of this instance.
        /// </summary>
        /// <value>
        /// <see cref="DateTime"/> that represents the created time of this instance.
        /// </value>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the detailed information of this instance.
        /// </summary>
        /// <value>
        /// <see cref="object"/> that contains the detailed information to write to the log for this instance.
        /// </value>
        public object Data { get; set; }

        /// <summary>
        /// Gets or sets the id of this instance.
        /// </summary>
        /// <value>
        /// <see cref="string"/> that represents the id of this instance.
        /// </value>
        public Guid Id { get; set; }

        #endregion
    }
}
