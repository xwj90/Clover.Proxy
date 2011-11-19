using System;
using System.Globalization;
using System.Resources;

namespace Clover.Proxy.OldDesign
{

    #region Using directives

    #endregion

    /// <summary>
    /// Provides helper operations to raise, convert or represent service exceptions. This class is static.
    /// </summary>
    //[DebuggerStepThrough]
    public static class ErrorService
    {
        #region Public Methods

        /// <summary>
        /// Converts a HRESULT value into a <see cref="ErrorCode"/> enumeration.
        /// </summary>
        /// <param name="value">
        /// The unsigned 32-bit integer HRESULT value to convert from.
        /// </param>
        /// <returns>
        /// <see cref="ErrorCode"/> that represents the specified HRESULT.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The specified <paramref name="value"/> is not defined in the <see cref="ErrorCode"/> enumeration.
        /// </exception>
        public static ErrorCode ConvertFromHResult(int value)
        {
            if (!Enum.IsDefined(typeof (ErrorCode), value))
            {
                //throw new ArgumentException(Errors.EnumValueNotDefined, "value");
                throw new ArgumentException("value");
            }

            return (ErrorCode) value;
        }

        /// <summary>
        /// Converts the specified error code to HRESULT (an unsigned 32-bit integer value).
        /// </summary>
        /// <param name="errorCode">
        /// The error code to convert to.
        /// </param>
        /// <returns>
        /// <see cref="int"/> that represents the error code in HRESULT format.
        /// </returns>
        public static int ConvertToHResult(ErrorCode errorCode)
        {
            return (int) errorCode;
        }

        /// <summary>
        /// Converts a specified <c>ErrorCode</c> to its string representation.
        /// </summary>
        /// <param name="errorCode">
        /// The error code to convert to.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> that represents the specified <paramref name="errorCode"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The specified <paramref name="errorCode"/> is not defined in the <see cref="ErrorCode"/> enumeration.
        /// </exception>
        public static string ConvertToString(ErrorCode errorCode)
        {
            return ConvertToString(errorCode, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Converts a specified integer to its string representation.
        /// </summary>
        /// <param name="value">
        /// The specified integer error code to convert to.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> that represents the specified <paramref name="value"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The specified <paramref name="value"/> is not defined in the <see cref="ErrorCode"/> enumeration.
        /// </exception>
        public static string ConvertToString(int value)
        {
            return ConvertToString(ConvertFromHResult(value), CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Converts a specified <c>ErrorCode</c> to its string representation in a specified culture.
        /// </summary>
        /// <param name="errorCode">
        /// The error code to convert to.
        /// </param>
        /// <param name="culture">
        /// The culture info specifying the culture information to convert to.
        /// </param>
        /// <returns>
        /// A culture specified <see cref="string"/> that represents the specified <paramref name="errorCode"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="culture"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The specified <paramref name="errorCode"/> is not defined in the <see cref="ErrorCode"/> enumeration.
        /// </exception>
        public static string ConvertToString(ErrorCode errorCode, CultureInfo culture)
        {
            string errorMessage = GetDescription(errorCode, culture);

            return string.Format(CultureInfo.CurrentCulture, "0x{0:X}: {1}", errorCode, errorMessage);
        }

        /// <summary>
        /// Gets the descriptive error message of a specified <c>ErrorCode</c>.
        /// </summary>
        /// <param name="errorCode">
        /// The error code to convert to.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> that represents the specified <paramref name="errorCode"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The specified <paramref name="errorCode"/> is not defined in the <see cref="ErrorCode"/> enumeration.
        /// </exception>
        public static string GetDescription(ErrorCode errorCode)
        {
            return GetDescription(errorCode, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Gets the descriptive error message of a specified HRESULT representation for a <see cref="ErrorCode"/>.
        /// </summary>
        /// <param name="value">
        /// The specified integer error code to convert to.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> that represents the specified <paramref name="value"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The specified <paramref name="value"/> is not defined in the <see cref="ErrorCode"/> enumeration.
        /// </exception>
        public static string GetDescription(int value)
        {
            return GetDescription(ConvertFromHResult(value), CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Gets the descriptive error message of a specified <c>ErrorCode</c> in a specified culture.
        /// </summary>
        /// <param name="errorCode">
        /// The error code to convert to.
        /// </param>
        /// <param name="culture">
        /// The culture info specifying the culture information to convert to.
        /// </param>
        /// <returns>
        /// A culture specified <see cref="string"/> that represents the specified <paramref name="errorCode"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="culture"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The specified <paramref name="errorCode"/> is not defined in the <see cref="ErrorCode"/> enumeration.
        /// </exception>
        public static string GetDescription(ErrorCode errorCode, CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }

            if (!Enum.IsDefined(typeof (ErrorCode), errorCode))
            {
                throw new ArgumentException("errorCode");
                //  throw new ArgumentException(Errors.EnumValueNotDefined, "errorCode");
            }

            string errorDescription = errorCode.ToString();
            string errorMessage = null;

            try
            {
                //  errorMessage = Errors.ResourceManager.GetString(errorDescription, culture);
            }
            catch (MissingManifestResourceException)
            {
                errorMessage = errorCode.ToString();
            }

            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                errorMessage = errorCode.ToString();
            }

            return errorMessage;
        }

        /// <summary>
        /// Creates an instance of a specified exception type with error code and inner exception.
        /// </summary>
        /// <typeparam name="T">
        /// The type of exception to create.
        /// </typeparam>
        /// <param name="errorCode">
        /// The error code to format as the exception message.
        /// </param>
        /// <param name="innerException">
        /// The inner exception to associate with.
        /// </param>
        /// <returns>
        /// <typeparamref name="T"/> that represents the created exception instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="innerException"/> is null.
        /// </exception>
        public static T CreateException<T>(ErrorCode errorCode, Exception innerException)
            where T : Exception
        {
            if (innerException == null)
            {
                throw new ArgumentNullException("innerException");
            }

            string errorMessage = ConvertToString(errorCode, CultureInfo.CurrentCulture);
            var exception = (T) Activator.CreateInstance(typeof (T), errorMessage, innerException);
            exception.Data.Add("ErrorCode", errorCode);

            return exception;
        }

        /// <summary>
        /// Creates an instance of a specified exception type with error code and format arguments.
        /// </summary>
        /// <typeparam name="T">
        /// The type of exception to create.
        /// </typeparam>
        /// <param name="errorCode">
        /// The error code to format as the exception message.
        /// </param>
        /// <param name="formatArgs">
        /// The format arguments.
        /// </param>
        /// <returns>
        /// <typeparamref name="T"/> that represents the created exception instance.
        /// </returns>
        public static T CreateException<T>(ErrorCode errorCode, params object[] formatArgs)
            where T : Exception
        {
            string errorMessage = FormatErrorCode(errorCode, CultureInfo.CurrentCulture, formatArgs);
            var exception = (T) Activator.CreateInstance(typeof (T), errorMessage);
            exception.Data.Add("ErrorCode", errorCode);

            return exception;
        }

        /// <summary>
        /// Creates an instance of a specified exception type with error code, inner exception and format arguments.
        /// </summary>
        /// <typeparam name="T">
        /// The type of exception to create.
        /// </typeparam>
        /// <param name="errorCode">
        /// The error code to format as the exception message.
        /// </param>
        /// <param name="innerException">
        /// The inner exception to associate with.
        /// </param>
        /// <param name="formatArgs">
        /// The format arguments.
        /// </param>
        /// <returns>
        /// <typeparamref name="T"/> that represents the created exception instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="innerException"/> is null.
        /// </exception>
        public static T CreateException<T>(ErrorCode errorCode, Exception innerException, params object[] formatArgs)
            where T : Exception
        {
            if (innerException == null)
            {
                throw new ArgumentNullException("innerException");
            }

            string errorMessage = FormatErrorCode(errorCode, CultureInfo.CurrentCulture, formatArgs);
            var exception = (T) Activator.CreateInstance(typeof (T), errorMessage, innerException);
            exception.Data.Add("ErrorCode", errorCode);

            return exception;
        }

        /// <summary>
        /// Creates an instance of a specified exception type with error code.
        /// </summary>
        /// <typeparam name="T">
        /// The type of exception to create.
        /// </typeparam>
        /// <param name="errorCode">
        /// The error code to format as the exception message.
        /// </param>
        /// <returns>
        /// <typeparamref name="T"/> that represents the created exception instance.
        /// </returns>
        public static T CreateException<T>(ErrorCode errorCode)
            where T : Exception
        {
            string errorMessage = ConvertToString(errorCode, CultureInfo.CurrentCulture);
            var exception = (T) Activator.CreateInstance(typeof (T), errorMessage);
            exception.Data.Add("ErrorCode", errorCode);

            return exception;
        }

        /// <summary>
        /// Formats the error code to a specific culture represented string with format arguments.
        /// </summary>
        /// <param name="errorCode">
        /// The error code to format.
        /// </param>
        /// <param name="culture">
        /// The culture information to format.
        /// </param>
        /// <param name="args">
        /// The string format arguments.
        /// </param>
        /// <returns>
        /// <see cref="string"/> that represents the formatted value in the specified culture.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="culture"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The specified <paramref name="errorCode"/> is not defined in the <see cref="ErrorCode"/> enumeration.
        /// </exception>
        public static string FormatErrorCode(ErrorCode errorCode, CultureInfo culture, params object[] args)
        {
            return string.Format(
                culture,
                ConvertToString(errorCode, culture),
                args);
        }

        #endregion
    }
}