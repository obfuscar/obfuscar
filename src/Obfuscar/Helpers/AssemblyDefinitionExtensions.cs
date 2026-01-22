using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;  // Add reference to NuGet.Versioning
using Obfuscar.Metadata.Abstractions;
using Obfuscar.Metadata.Mutable;

namespace Obfuscar.Helpers
{
    public static class AssemblyDefinitionExtensions
    {
        public static string GetPortableProfileDirectory(this MutableAssemblyDefinition assembly)
        {
            foreach (var custom in assembly.CustomAttributes)
            {
                if (custom.AttributeTypeName == "System.Runtime.Versioning.TargetFrameworkAttribute")
                {
                    var displayName = Helper.GetAttributePropertyByName(custom, "FrameworkDisplayName")?.ToString();
                    if (string.IsNullOrEmpty(displayName))
                        continue;
                    if (!string.Equals(displayName, ".NET Portable Subset"))
                    {
                        return null;
                    }

                    var ctorValue = custom.ConstructorArguments?.FirstOrDefault()?.Value?.ToString();
                    if (string.IsNullOrEmpty(ctorValue))
                        return null;
                    var parts = ctorValue.Split(',');
                    var root = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                    return Environment.ExpandEnvironmentVariables(
                        Path.Combine(
                            root,
                            "Reference Assemblies",
                            "Microsoft",
                            "Framework",
                            parts[0],
                            (parts[1].Split('='))[1],
                            "Profile",
                            (parts[2].Split('='))[1]));
                }
            }

            return null;
        }

        public static IEnumerable<string> GetNetCoreDirectories(this MutableAssemblyDefinition assembly)
        {
            var seenFrameworks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Local helper to detect dotnet packs roots (DOTNET_ROOT, ProgramFiles, dotnet --info, user NuGet)
            static IEnumerable<string> DetectDotnetPacksRoots()
            {
                var results = new List<string>();

                if (Environment.OSVersion.Platform == PlatformID.Unix || 
                    Environment.OSVersion.Platform == PlatformID.MacOSX)
                {
                    results.Add("/usr/local/share/dotnet/packs");
                }
                else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    var pf = Environment.GetEnvironmentVariable("ProgramFiles");
                    if (!string.IsNullOrEmpty(pf))
                        results.Add(Path.Combine(pf, "dotnet", "packs"));

                    var pf86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                    if (!string.IsNullOrEmpty(pf86))
                        results.Add(Path.Combine(pf86, "dotnet", "packs"));
                }

                var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? Environment.GetEnvironmentVariable("DOTNET_HOME");
                if (!string.IsNullOrEmpty(dotnetRoot))
                {
                    results.Add(Path.Combine(dotnetRoot, "packs"));
                }

                return results.Distinct(StringComparer.OrdinalIgnoreCase);
            }

            var packsRoots = DetectDotnetPacksRoots().ToList();

            foreach (var custom in assembly.CustomAttributes)
            {
                if (custom.AttributeTypeName == "System.Runtime.Versioning.TargetFrameworkAttribute")
                {
                    var framework = custom.ConstructorArguments?.FirstOrDefault()?.Value?.ToString() ?? string.Empty;

                    // Normalize framework string (e.g., ".NETCoreApp,Version=6.0" -> ".NETCoreApp,Version=v6.0")
                    if (framework.StartsWith(".NETCoreApp,Version=") && !framework.Contains("v"))
                    {
                        framework = framework.Replace("Version=", "Version=v");
                    }

                    // Skip if this framework has already been processed
                    if (!seenFrameworks.Add(framework))
                        continue;

                    // Handle .NET Core
                    if (framework.StartsWith(".NETCoreApp,Version="))
                    {
                        var versionStr = framework.Split('=')[1].Substring(1);

                        string[] profiles = new[]
                        {
                            "Microsoft.AspNetCore.App.Ref",
                            "Microsoft.NETCore.App.Ref",
                            "Microsoft.WindowsDesktop.App.Ref"
                        };

                        foreach (var profile in profiles)
                        {
                            // Try detected packs roots first, then fall back to traditional ProgramFiles location.
                            var tried = new List<string>();
                            foreach (var root in packsRoots)
                            {
                                var baseDir = Path.Combine(root, profile);
                                tried.Add(baseDir);
                                if (Directory.Exists(baseDir))
                                {
                                    yield return Path.Combine(FindBestVersionMatch(baseDir, versionStr), "ref", $"net{versionStr}");
                                    // yield once per matching root and stop for this profile
                                    break;
                                }
                            }
                        }
                    }
                    // Handle .NET Standard
                    else if (framework.StartsWith(".NETStandard,Version="))
                    {
                        var versionStr = framework.Split('=')[1].Substring(1);

                        if (Version.TryParse(versionStr, out Version parsedVersion) && parsedVersion <= new Version(2, 0))
                        {
                            // For .NET Standard 1.x and 2.0, check the NuGet fallback folder
                            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) ?? Environment.GetEnvironmentVariable("HOME");
                            string nugetPackagesPath = Path.Combine(homeDir, ".nuget", "packages", "netstandard.library");

                            if (Directory.Exists(nugetPackagesPath))
                            {
                                string bestVersion = FindBestNuGetVersionMatch(nugetPackagesPath, versionStr);
                                if (!string.IsNullOrEmpty(bestVersion))
                                {
                                    yield return Path.Combine(nugetPackagesPath, bestVersion, "build", $"netstandard{versionStr}", "ref");
                                }
                            }
                        }
                        else
                        {
                            // For .NET Standard 2.1 and above, check the .NET SDK packs directory
                            // Try detected packs roots for NETStandard.Library.Ref
                            foreach (var root in packsRoots)
                            {
                                var baseDir = Path.Combine(root, "NETStandard.Library.Ref");
                                if (Directory.Exists(baseDir))
                                {
                                    yield return Path.Combine(FindBestVersionMatch(baseDir, versionStr), "ref", $"netstandard{versionStr}");
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds the best matching version directory for a given major.minor version
        /// </summary>
        private static string FindBestVersionMatch(string baseDir, string versionStr)
        {
            if (!Directory.Exists(baseDir))
                return Path.Combine(baseDir, versionStr);

            // Get all version directories
            var allDirs = Directory.GetDirectories(baseDir);
            
            // Parse the requested version
            var requestedVersion = new NuGetVersion(versionStr);
            
            // Find matching directories with the same major.minor version
            var matchingDirs = new List<(string Path, NuGetVersion Version)>();
            
            foreach (var dir in allDirs)
            {
                var dirName = Path.GetFileName(dir);
                if (NuGetVersion.TryParse(dirName, out var dirVersion) && 
                    dirVersion.Major == requestedVersion.Major && 
                    dirVersion.Minor == requestedVersion.Minor)
                {
                    matchingDirs.Add((dir, dirVersion));
                }
            }
            
            // If there are no matching directories, fall back to exact version
            if (matchingDirs.Count == 0)
            {
                // Look for exact match
                var exactMatch = allDirs.FirstOrDefault(d => Path.GetFileName(d) == versionStr);
                if (exactMatch != null)
                    return exactMatch;
                
                // If no exact match exists either, return constructed path
                return Path.Combine(baseDir, $"{versionStr}.0");
            }
            
            // Sort directories by version and return the highest one
            // Stable versions are preferred over prerelease ones
            var bestMatch = matchingDirs
                .OrderByDescending(x => x.Version, VersionComparer.Default)
                .First();
            
            return bestMatch.Path;
        }
        
        /// <summary>
        /// Finds the best matching version directory for a given major.minor version in NuGet packages
        /// </summary>
        private static string FindBestNuGetVersionMatch(string baseDir, string versionStr)
        {
            if (!Directory.Exists(baseDir))
                return null;

            // Get all version directories
            var allDirs = Directory.GetDirectories(baseDir);
            
            // Parse the requested version
            if (!NuGetVersion.TryParse(versionStr, out var requestedVersion))
                return null;
            
            // Find matching directories with the same major.minor version
            var matchingDirs = new List<(string Path, NuGetVersion Version)>();
            
            foreach (var dir in allDirs)
            {
                var dirName = Path.GetFileName(dir);
                if (NuGetVersion.TryParse(dirName, out var dirVersion) && 
                    dirVersion.Major == requestedVersion.Major && 
                    dirVersion.Minor == requestedVersion.Minor)
                {
                    matchingDirs.Add((dir, dirVersion));
                }
            }
            
            // If there are no matching directories, fall back to exact version
            if (matchingDirs.Count == 0)
            {
                // Look for exact match
                var exactMatch = allDirs.FirstOrDefault(d => NuGetVersion.TryParse(Path.GetFileName(d), out var v) && v == requestedVersion);
                if (exactMatch != null)
                    return Path.GetFileName(exactMatch);
                
                // If no exact match exists either, return null
                return null;
            }
            
            // Sort directories by version and return the highest one
            // Stable versions are preferred over prerelease ones by default with VersionComparer
            var bestMatch = matchingDirs
                .OrderByDescending(x => x.Version, VersionComparer.Default)
                .First();
            
            return Path.GetFileName(bestMatch.Path);
        }

        public static bool MarkedToRename(this MutableAssemblyDefinition assembly)
        {
            foreach (var custom in assembly.CustomAttributes)
            {
                if (custom.AttributeTypeName == typeof(ObfuscateAssemblyAttribute).FullName)
                {
                    var rename = (bool)(Helper.GetAttributePropertyByName(custom, "AssemblyIsPrivate") ?? true);
                    return rename;
                }
            }

            // IMPORTANT: assume it should be renamed.
            return true;
        }

        public static bool CleanAttributes(this MutableAssemblyDefinition assembly)
        {
            RemoveObfuscateAssemblyAttributes(assembly.CustomAttributes);
            if (assembly.MainModule != null)
                RemoveObfuscateAssemblyAttributes(assembly.MainModule.CustomAttributes);

            return true;
        }

        private static void RemoveObfuscateAssemblyAttributes(List<MutableCustomAttribute> attributes)
        {
            if (attributes == null || attributes.Count == 0)
                return;

            for (int i = attributes.Count - 1; i >= 0; i--)
            {
                var custom = attributes[i];
                if (custom.AttributeTypeName == typeof(ObfuscateAssemblyAttribute).FullName)
                {
                    if ((bool)(Helper.GetAttributePropertyByName(custom, "StripAfterObfuscation") ?? true))
                    {
                        attributes.RemoveAt(i);
                    }
                }
            }
        }
    }
}
