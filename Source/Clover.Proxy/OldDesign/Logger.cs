using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.IO.Log;
using System.Reflection;

namespace Clover.Proxy.OldDesign
{

    #region Using directives

    #endregion

    /// <summary>
    /// Encapsulates a type to write application logs based on a Common Log File System. This class cannot be inherited.
    /// </summary>
    public sealed class Logger : IDisposable
    {
        #region Constants and Fields

        /// <summary>
        /// The key of the configuration for base name.
        /// </summary>
        public const string BaseNameKey = "Clover.LogBaseName";

        /// <summary>
        /// The key of the configuration for base directory. 
        /// </summary>
        public const string BasePathKey = "Clover.LogBasePath";

        /// <summary>
        /// The key of the configuration for extent size.
        /// </summary>
        public const string ExtentSizeKey = "Clover.ExtentSize";

        /// <summary>
        /// The default size for each log extent.
        /// </summary>
        private const int DefaultExtentSize = 10*1024*1024;

        /// <summary>
        /// The default instance of this class.
        /// </summary>
        private static Logger defaultLogger;

        /// <summary>
        /// automatic disable logger feature 
        /// </summary>
        private static bool AutomaticDisable;

        /// <summary>
        /// The log record sequence object to write the log entries.
        /// </summary>
        private readonly LogRecordSequence _sequence;

        /// <summary>
        /// The log store to hold the log record sequence object.
        /// </summary>
        private readonly LogStore _store;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="basePath">
        /// The base path where the log files are resided.
        /// </param>
        /// <param name="baseName">
        /// The base name of the log files.
        /// </param>
        /// <param name="extentSize">
        /// Size of each log extent in bytes.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="basePath"/> is null, or <paramref name="baseName"/> is null.
        /// </exception>
        public Logger(string basePath, string baseName, int extentSize)
        {
            if (string.IsNullOrEmpty(basePath))
            {
                throw new ArgumentNullException("basePath");
            }

            if (string.IsNullOrEmpty(baseName))
            {
                throw new ArgumentNullException("baseName");
            }

            // If the base path doesn't exist, create it.
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            string fullPath = Path.Combine(basePath, baseName);
            try
            {
                _store = new LogStore(fullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            }
            catch (PlatformNotSupportedException)
            {
                AutomaticDisable = true;
                return;
            }
            _sequence = new LogRecordSequence(_store);
            _sequence.RetryAppend = true;

            if (_store.Extents.Count == 0)
            {
                _store.Extents.Add(fullPath, extentSize);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="basePath">
        /// The base path where the log files are resided.
        /// </param>
        /// <param name="baseName">
        /// The base name of the log files.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="basePath"/> is null, or <paramref name="baseName"/> is null.
        /// </exception>
        public Logger(string basePath, string baseName)
            : this(basePath, baseName, DefaultExtentSize)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class by reading the arguments from the application 
        /// configuration file.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// One or more configuration values are not found in the application's configuration file.
        /// </exception>
        public Logger()
            : this(
                ConfigurationManager.AppSettings[BasePathKey],
                ConfigurationManager.AppSettings[BaseNameKey],
                Convert.ToInt32(ConfigurationManager.AppSettings[ExtentSizeKey], CultureInfo.CurrentCulture))
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the default instance of the <see cref="Logger"/> class.
        /// </summary>
        public static Logger Current
        {
            get
            {
                if (defaultLogger == null)
                {
                    if (IsConfigured)
                    {
                        defaultLogger = new Logger();
                    }
                    else
                    {
                        defaultLogger = new Logger(Environment.CurrentDirectory,
                                                   Assembly.GetCallingAssembly().GetName().Name,
                                                   DefaultExtentSize);
                    }
                }

                return defaultLogger;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the logger is configured in the application configuration file.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is configured; otherwise, <c>false</c>.
        /// </value>
        public static bool IsConfigured
        {
            get
            {
                return !string.IsNullOrEmpty(ConfigurationManager.AppSettings[BaseNameKey])
                       && !string.IsNullOrEmpty(ConfigurationManager.AppSettings[BasePathKey])
                       && !string.IsNullOrEmpty(ConfigurationManager.AppSettings[ExtentSizeKey]);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Writes the specified entry to the log store with format arguments.
        /// </summary>
        /// <param name="entry">
        /// The entry to write.
        /// </param>
        /// <param name="args">
        /// The arguments to format the entry.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entry"/> is null.
        /// </exception>
        public void WriteEntry(string entry, params object[] args)
        {
            WriteEntry(LogCategory.Info, entry, args);
        }

        /// <summary>
        /// Writes the specified entry to the log store with format arguments.
        /// </summary>
        /// <param name="entry">
        /// The entry to write.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entry"/> is null.
        /// </exception>
        public void WriteEntry(string entry)
        {
            WriteEntry(LogCategory.Info, entry);
        }

        /// <summary>
        /// Writes the specified entry to the log store with format arguments.
        /// </summary>
        /// <param name="category">
        /// The category of the log entry.
        /// </param>
        /// <param name="entry">
        /// The entry to write.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entry"/> is null.
        ///   </exception>
        public void WriteEntry(LogCategory category, string entry)
        {
            if (AutomaticDisable)
            {
                return;
            }
            if (string.IsNullOrEmpty(entry))
            {
                throw new ArgumentNullException("entry");
            }

            var logEntry = new LogEntry(category, entry);
            WriteEntry(logEntry);
        }

        /// <summary>
        /// Writes the specified entry to the log store with category and format arguments.
        /// </summary>
        /// <param name="category">
        /// The category of the log entry.
        /// </param>
        /// <param name="entry">
        /// The entry to write.
        /// </param>
        /// <param name="args">
        /// The arguments to format the entry.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entry"/> is null.
        /// </exception>
        public void WriteEntry(LogCategory category, string entry, params object[] args)
        {
            if (AutomaticDisable)
            {
                return;
            }
            if (string.IsNullOrEmpty(entry))
            {
                throw new ArgumentNullException("entry");
            }

            entry = string.Format(CultureInfo.CurrentCulture, entry, args);
            var logEntry = new LogEntry(category, entry);
            WriteEntry(logEntry);
        }

        /// <summary>
        /// Writes the specified entry to the log store.
        /// </summary>
        /// <param name="entry">
        /// The entry to write to the log store.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entry"/> is null.
        /// </exception>
        public void WriteEntry(LogEntry entry)
        {
            if (AutomaticDisable)
            {
                return;
            }
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            byte[] bytes = entry.ToSerializedByteArray();
            _sequence.Append(new ArraySegment<byte>(bytes), SequenceNumber.Invalid, SequenceNumber.Invalid,
                             RecordAppendOptions.ForceFlush);
        }

        /// <summary>
        /// Writes the specified entry to the log store.
        /// </summary>
        /// <param name="exception">
        /// The exception to write to the log store.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="exception"/> is null.
        /// </exception>
        public void WriteEntry(Exception exception)
        {
            if (AutomaticDisable)
            {
                return;
            }
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            var entry = new LogEntry(LogCategory.Error, exception.ToString());
            WriteEntry(entry);
        }

        #endregion

        #region Implemented Interfaces

        #region IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
        /// </param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_sequence != null)
                {
                    _sequence.Dispose();
                }

                if (_store != null)
                {
                    _store.Dispose();
                }
            }
        }

        #endregion
    }
}