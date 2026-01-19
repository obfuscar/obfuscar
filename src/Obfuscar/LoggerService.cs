using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Obfuscar
{
    /// <summary>
    /// Logging service for Obfuscar
    /// </summary>
    public static class LoggerService
    {
        private static ILogger _logger = NullLogger.Instance;

        /// <summary>
        /// Gets the current logger instance
        /// </summary>
        public static ILogger Logger => _logger;

        /// <summary>
        /// Sets the logger instance to be used throughout the library
        /// </summary>
        /// <param name="logger">The logger implementation to use</param>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger ?? NullLogger.Instance;
        }
    }
}
