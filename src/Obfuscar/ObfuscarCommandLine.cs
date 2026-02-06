#region Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>

/// <copyright>
/// Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// </copyright>

#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Obfuscar
{
    /// <summary>
    /// A simple empty scope for use with the logger
    /// </summary>
    internal class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable Instance { get; } = new EmptyDisposable();
        
        private EmptyDisposable() { }
        
        public void Dispose() { }
    }
    
    /// <summary>
    /// A custom console logger provider that excludes the category name and event ID
    /// </summary>
    public class ObfuscarLoggerProvider : ILoggerProvider
    {
        private readonly LogLevel _minimumLevel;

        public ObfuscarLoggerProvider(LogLevel minimumLevel = LogLevel.Information)
        {
            _minimumLevel = minimumLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new ObfuscarConsoleLogger(categoryName, _minimumLevel);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }

    /// <summary>
    /// A custom logger that displays messages without the category name and event ID
    /// </summary>
    public class ObfuscarConsoleLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly LogLevel _minimumLevel;

        public ObfuscarConsoleLogger(string categoryName, LogLevel minimumLevel = LogLevel.Information)
        {
            _categoryName = categoryName;
            _minimumLevel = minimumLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return EmptyDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && logLevel >= _minimumLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string message = formatter(state, exception);
            
            // Get the appropriate color based on log level
            ConsoleColor color = GetColorForLogLevel(logLevel);
            
            // Change color, write prefix, restore color
            Console.ForegroundColor = color;
            Console.Write(GetLogLevelString(logLevel));
            Console.ResetColor();
            
            // Write message
            Console.WriteLine($": {message}");
            
            // Write exception details if any
            if (exception != null)
            {
                Console.WriteLine(exception.ToString());
            }
        }
        
        public static string GetLogLevelString(LogLevel logLevel)
        {
            if (logLevel == LogLevel.Trace) return "trce";
            if (logLevel == LogLevel.Debug) return "dbug";
            if (logLevel == LogLevel.Information) return "info";
            if (logLevel == LogLevel.Warning) return "warn";
            if (logLevel == LogLevel.Error) return "fail";
            if (logLevel == LogLevel.Critical) return "crit";
            return "????";
        }
        
        private static ConsoleColor GetColorForLogLevel(LogLevel logLevel)
        {
            if (logLevel == LogLevel.Trace || logLevel == LogLevel.Debug) return ConsoleColor.Gray;
            if (logLevel == LogLevel.Information) return ConsoleColor.Green;
            if (logLevel == LogLevel.Warning) return ConsoleColor.Yellow;
            if (logLevel == LogLevel.Error) return ConsoleColor.Red;
            if (logLevel == LogLevel.Critical) return ConsoleColor.DarkRed;
            return ConsoleColor.Gray;
        }
    }

    /// <summary>
    /// Represents parsed command-line arguments
    /// </summary>
    public class ParsedArguments
    {
        public bool ShowHelp { get; set; }
        public bool ShowVersion { get; set; }
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
        public List<string> ProjectFiles { get; set; } = new List<string>();
    }

    /// <summary>
    /// Shared command-line utilities for the Obfuscar Console and .NET Global Tools
    /// </summary>
    public static class ObfuscarCommandLine
    {
        /// <summary>
        /// Sets up the command line parser and processes the arguments
        /// </summary>
        public static Task<int> RunAsync(string[] args, string appName, string appDescription, string appCopyright)
        {
            try
            {
                // Parse the command line arguments
                var parsedArgs = ParseCommandLine(args);
                
                // Configure logging early so we can use it for help/version messages
                ILogger logger = ConfigureLogging(parsedArgs.LogLevel);

                // Show help if requested or no arguments provided
                if (parsedArgs.ShowHelp || args.Length == 0)
                {
                    ShowHelp(appName, appCopyright);
                    return Task.FromResult(0);
                }

                // Show version if requested
                if (parsedArgs.ShowVersion)
                {
                    logger.LogInformation("{Version}", Assembly.GetEntryAssembly().GetName().Version);
                    return Task.FromResult(0);
                }

                // Validate that we have project files
                if (parsedArgs.ProjectFiles.Count < 1)
                {
                    ShowHelp(appName, appCopyright);
                    logger.LogError("No project files specified. Please provide at least one project file to process.");
                    return Task.FromResult(1);
                }
                
                // Run obfuscation
                return Task.FromResult(RunObfuscation(parsedArgs.ProjectFiles, parsedArgs.LogLevel, logger));
            }
            catch (Exception ex)
            {
                var logger = LoggerService.Logger;
                logger.LogError(ex, "Error: {Message}", ex.Message);
                return Task.FromResult(1);
            }
        }
        
        /// <summary>
        /// Parses the command line arguments
        /// </summary>
        private static ParsedArguments ParseCommandLine(string[] args)
        {
            var parsedArgs = new ParsedArguments();
            
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                
                if (arg.StartsWith("-") || arg.StartsWith("--"))
                {
                    // Check for help option
                    if (arg == "-h" || arg == "-?" || arg == "--help")
                    {
                        parsedArgs.ShowHelp = true;
                    }
                    // Check for version option
                    else if (arg == "-V" || arg == "--version")
                    {
                        parsedArgs.ShowVersion = true;
                    }
                    // Handle format with colon: --verbosity:detailed or -v:d
                    else if (arg.StartsWith("--verbosity:") || arg.StartsWith("-v:"))
                    {
                        string verbosityArg = arg.Contains(":") ? arg.Substring(arg.IndexOf(':') + 1) : string.Empty;
                        parsedArgs.LogLevel = ParseVerbosityLevel(verbosityArg);
                    }
                }
                else
                {
                    // Not an option, must be a project file
                    parsedArgs.ProjectFiles.Add(arg);
                }
            }
            
            return parsedArgs;
        }
        
        /// <summary>
        /// Show the application banner and help information
        /// </summary>
        private static void ShowHelp(string appName, string appCopyright)
        {
            var logger = LoggerService.Logger;
            logger.LogInformation("{AppName} is available at https://www.obfuscar.com", appName);
            logger.LogInformation(appCopyright);
            logger.LogInformation(string.Empty);
            logger.LogInformation("Usage: obfuscar [Options] <project_file> [project_file...]");
            logger.LogInformation(string.Empty);
            logger.LogInformation("Options:");
            logger.LogInformation("  -v:<level>, --verbosity:<level>  Set verbosity level (q=quiet, m=minimal, n=normal, d=detailed, diag=diagnostic)");
            logger.LogInformation("  -V, --version                   Display version information");
            logger.LogInformation("  -h, --help                      Show help information");
            logger.LogInformation(string.Empty);
        }
        
        /// <summary>
        /// Parse the verbosity level from the command line argument
        /// </summary>
        private static LogLevel ParseVerbosityLevel(string verbosity)
        {
            if (string.IsNullOrEmpty(verbosity))
                return LogLevel.Information;
                
            switch (verbosity.ToLowerInvariant())
            {
                case "q":
                case "quiet":
                    return LogLevel.Error;
                case "m":
                case "minimal":
                    return LogLevel.Warning;
                case "n":
                case "normal":
                    return LogLevel.Information;
                case "d":
                case "detailed":
                    return LogLevel.Debug;
                case "diag":
                case "diagnostic":
                    return LogLevel.Trace;
                default:
                    return LogLevel.Information;
            }
        }

        /// <summary>
        /// Creates and configures a logger for Obfuscar
        /// </summary>
        public static ILogger ConfigureLogging(LogLevel logLevel)
        {
            // Use a lightweight direct logger to avoid LoggerFactory startup overhead.
            ILogger logger = new ObfuscarConsoleLogger("Obfuscar", logLevel);

            // Set the global logger for the Obfuscar library
            LoggerService.SetLogger(logger);

            return logger;
        }

        /// <summary>
        /// Runs Obfuscar on the specified project files
        /// </summary>
        public static int RunObfuscation(List<string> projectFiles, LogLevel logLevel, ILogger logger)
        {
            if (projectFiles.Count < 1)
            {
                return 1;
            }

            logger.LogInformation("Obfuscar starting with log level: {0}", ObfuscarConsoleLogger.GetLogLevelString(logLevel));
            logger.LogInformation("Use -v:<level> to set verbosity level (q=quiet, m=minimal, n=normal, d=detailed, diag=diagnostic)");
            
            int start = Environment.TickCount;
            foreach (var project in projectFiles)
            {
                try
                {
                    logger.LogInformation("Loading project {0}...", project);
                    Obfuscator obfuscator = new Obfuscator(project);
                    logger.LogInformation("Project loaded successfully");

                    obfuscator.RunRules();

                    logger.LogInformation("Completed in {0:f2} seconds", (Environment.TickCount - start) / 1000.0);
                }
                catch (ObfuscarException e)
                {
                    logger.LogError(e, "An error occurred during processing");
                    logger.LogError("{0}", e.Message);
                    if (e.InnerException != null)
                        logger.LogError("{0}", e.InnerException.Message);
                    return 1;
                }
            }

            return 0;
        }
    }
}
