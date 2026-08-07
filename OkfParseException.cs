namespace Okf.Net.Exceptions
{


    public class OkfParseException : Exception
    {
        public string FilePath { get; }

        public OkfParseException(string message, string filePath)
            : base($"Error parsing OKF Document '{filePath}': {message}")
        {
            FilePath = filePath;
        }

        public OkfParseException(string message, string filePath, Exception innerException)
            : base($"Error parsing OKF Document '{filePath}': {message}", innerException)
        {
            FilePath = filePath;
        }
    }
}