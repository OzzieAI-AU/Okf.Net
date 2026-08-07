namespace Okf.Net.Exceptions
{


    /// <summary>
    /// Represents the exception that is thrown when errors occur during the parsing of an OKF (Open Knowledge Format) document.
    /// </summary>
    /// <remarks>
    /// This exception is typically raised by the parser when it encounters invalid syntax, structural violations, 
    /// or unreadable data in an OKF file. It captures the target file path to facilitate diagnostics and troubleshooting.
    /// </remarks>
    public class OkfParseException : Exception
    {


        /// <summary>
        /// Gets the file path of the OKF document that caused the parsing exception.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> representing the absolute or relative file path where the parsing failure occurred.
        /// </value>
        public string FilePath { get; }


        /// <summary>
        /// Initializes a new instance of the <see cref="OkfParseException"/> class with a specified error message 
        /// and the file path of the document that failed to parse.
        /// </summary>
        /// <param name="message">The message that describes the specific parsing error.</param>
        /// <param name="filePath">The file path of the OKF document being parsed when the error occurred.</param>
        public OkfParseException(string message, string filePath)
            : base($"Error parsing OKF Document '{filePath}': {message}")
        {
            FilePath = filePath;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="OkfParseException"/> class with a specified error message, 
        /// the file path of the document, and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the specific parsing error.</param>
        /// <param name="filePath">The file path of the OKF document being parsed when the error occurred.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a <see langword="null"/> reference if no inner exception is specified.</param>
        public OkfParseException(string message, string filePath, Exception innerException)
            : base($"Error parsing OKF Document '{filePath}': {message}", innerException)
        {
            FilePath = filePath;
        }
    }
}