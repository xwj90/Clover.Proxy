
namespace Clover.Proxy
{
    #region Using directives

    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    #endregion

    /// <summary>
    /// Provides extension methods for object binary serializations. This class is static.
    /// </summary>
    internal static class BinaryExtensions
    {
        #region Constants and Fields

        /// <summary>
        /// The default object binary formatter to use for binary serialization/deserialization.
        /// </summary>
        private static readonly BinaryFormatter BinaryFormatter = new BinaryFormatter();

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns an array of binary serialized bytes for the current object.
        /// </summary>
        /// <param name="source">
        /// The source object.
        /// </param>
        /// <returns>
        /// A byte array that is binary serialized.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is null.
        /// </exception>
        internal static byte[] ToSerializedByteArray(this object source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            using (MemoryStream stream = new MemoryStream())
            {
                BinaryExtensions.BinaryFormatter.Serialize(stream, source);
                return stream.ToArray();
            }
        }

        #endregion
    }
}
