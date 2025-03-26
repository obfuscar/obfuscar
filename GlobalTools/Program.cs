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
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Mono.Options;

namespace Obfuscar
{
    internal static class Program
    {
        private static void ShowHelp(OptionSet optionSet)
        {
            Console.WriteLine("Obfuscar for .NET Core is available at https://www.obfuscar.com");
            Console.WriteLine("(C) 2007-2024, Ryan Williams and other contributors.");
            Console.WriteLine();
            Console.WriteLine("obfuscar [Options] [project_file] [project_file]");
            Console.WriteLine("Options:");
            optionSet.WriteOptionDescriptions(Console.Out);
        }

        private static int Main(string[] args)
        {
            bool showHelp = false;
            bool showVersion = false;
            LogLevel logLevel = LogLevel.Information; // Default log level

            OptionSet p = new OptionSet()
                .Add("h|?|help", "Print this help information.", delegate (string v) { showHelp = v != null; })
                .Add("V|version", "Display version number of this application.",
                    delegate (string v) { showVersion = v != null; })
                .Add("v:|verbosity:", "Set logging verbosity (q|quiet|n|normal|v|verbose|t|trace)",
                    delegate(string v)
                    {
                        if (string.IsNullOrEmpty(v))
                            return;
                            
                        switch (v.ToLowerInvariant())
                        {
                            case "q":
                            case "quiet":
                                logLevel = LogLevel.Warning;
                                break;
                            case "n":
                            case "normal":
                                logLevel = LogLevel.Information;
                                break;
                            case "v":
                            case "verbose":
                                logLevel = LogLevel.Debug;
                                break;
                            case "t":
                            case "trace":
                                logLevel = LogLevel.Trace;
                                break;
                        }
                    });

            if (args.Length == 0)
            {
                ShowHelp(p);
                return 0;
            }

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }

            if (showHelp)
            {
                ShowHelp(p);
                return 0;
            }

            if (showVersion)
            {
                Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version);
                return 0;
            }

            if (extra.Count < 1)
            {
                ShowHelp(p);
                return 1;
            }

            // Configure logging based on verbosity level
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(logLevel)
                    .AddConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.TimestampFormat = "[HH:mm:ss] ";
                    });
            });

            // Create logger for the Program class
            ILogger logger = loggerFactory.CreateLogger("Obfuscar");
            
            // Set the global logger for the Obfuscar library
            LoggerService.SetLogger(logger);

            logger.LogInformation("Obfuscar starting with log level: {LogLevel}", logLevel);
            
            int start = Environment.TickCount;
            foreach (var project in extra)
            {
                try
                {
                    logger.LogInformation("Loading project {ProjectFile}...", project);
                    Obfuscator obfuscator = new Obfuscator(project);
                    logger.LogInformation("Project loaded successfully");

                    obfuscator.RunRules();

                    logger.LogInformation("Completed in {ElapsedTime:f2} seconds", (Environment.TickCount - start) / 1000.0);
                }
                catch (ObfuscarException e)
                {
                    logger.LogError(e, "An error occurred during processing");
                    logger.LogError("{ErrorMessage}", e.Message);
                    if (e.InnerException != null)
                        logger.LogError("{InnerErrorMessage}", e.InnerException.Message);
                    return 1;
                }
            }

            return 0;
        }
    }
}
