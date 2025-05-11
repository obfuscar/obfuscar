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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace ObfuscarTests
{
    static class TestHelper
    {
        public static string InputPath = Path.Combine("..", "..", "..", "Input");

        private static int count;

        public static string OutputPath
        {
            get { return Path.Combine("..", "..", "..", "Output", (count++).ToString()); }
        }

        public static void CleanInput()
        {
            // clean out inputPath
            try
            {
                foreach (string file in Directory.GetFiles(InputPath, "*.dll"))
                File.Delete(file);
            }
            catch
            {
            }
        }
        
        public static void BuildAssembly(string name, string suffix, LanguageVersion languageVersion)
        {
            BuildAssembly(name, suffix, null, null, false, true, languageVersion);
        }

        public static void BuildAssembly(string name, string suffix = null, List<string> customReferences = null,
            string keyFile = null, bool delaySign = false, bool treatWarningsAsErrors = true,
            LanguageVersion languageVersion = LanguageVersion.Latest, bool useNetFramework = true,
            string targetFrameworkVersion = "net48")
        {
            string dllName = string.IsNullOrEmpty(suffix) ? name : name + suffix;

            string fileName = GetAssemblyPath(dllName);
            if (File.Exists(fileName))
            {
                return;
            }

            string code = File.ReadAllText(Path.Combine(InputPath, name + ".cs"));
            // Parse with specified language version (default is C# 10 to avoid extra attributes in assemblies)
            var tree = SyntaxFactory.ParseSyntaxTree(code, new CSharpParseOptions(languageVersion));

            List<MetadataReference> references = new List<MetadataReference>();

            if (useNetFramework)
            {
                // Find the reference assemblies from NuGet package
                // Typically they're in a path like:
                // ~/.nuget/packages/microsoft.netframework.referenceassemblies.net48/1.0.3/build/.NETFramework/v4.8/
                string nugetPackagesPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".nuget", "packages");
                    
                // This finds the most recent version of the package
                string referenceAssemblyPackageDir = Directory.GetDirectories(
                    Path.Combine(nugetPackagesPath, 
                    $"microsoft.netframework.referenceassemblies.{targetFrameworkVersion}"))
                    .OrderByDescending(d => d)
                    .FirstOrDefault();
                    
                if (referenceAssemblyPackageDir == null)
                {
                    throw new DirectoryNotFoundException(
                        $"Could not find Microsoft.NETFramework.ReferenceAssemblies.{targetFrameworkVersion} NuGet package.");
                }
                
                string frameworkPath = Path.Combine(
                    referenceAssemblyPackageDir,
                    "build", ".NETFramework", 
                    "v" + targetFrameworkVersion.Substring(3, 1) + "." + targetFrameworkVersion.Substring(4));
                    
                // Add core .NET Framework references
                references.Add(MetadataReference.CreateFromFile(Path.Combine(frameworkPath, "mscorlib.dll")));
                references.Add(MetadataReference.CreateFromFile(Path.Combine(frameworkPath, "System.dll")));
                references.Add(MetadataReference.CreateFromFile(Path.Combine(frameworkPath, "System.Core.dll")));
                references.Add(MetadataReference.CreateFromFile(Path.Combine(frameworkPath, "System.Runtime.Serialization.dll")));
                
                // Add other references that your code might need
                string facadesPath = Path.Combine(frameworkPath, "Facades");
                if (Directory.Exists(facadesPath))
                {
                    references.Add(MetadataReference.CreateFromFile(
                        Path.Combine(facadesPath, "System.Runtime.dll")));
                }
            }
            else
            {
                // Use the current runtime's references as before
                var systemRefLocation = typeof(object).GetTypeInfo().Assembly.Location;
                references.Add(MetadataReference.CreateFromFile(systemRefLocation));
                
                var dataContractRefLocation = typeof(DataContractAttribute).GetTypeInfo().Assembly.Location;
                references.Add(MetadataReference.CreateFromFile(dataContractRefLocation));
                
                var consoleRefLocation = typeof(Console).GetTypeInfo().Assembly.Location;
                references.Add(MetadataReference.CreateFromFile(consoleRefLocation));
                
                var attributeRefLocation = Path.Combine(Path.GetDirectoryName(systemRefLocation), "System.Runtime.dll");
                references.Add(MetadataReference.CreateFromFile(attributeRefLocation));
            }

            // Configure compilation options with the key file if provided
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            if (!string.IsNullOrEmpty(keyFile))
            {
                var fullPath = Path.GetFullPath(keyFile);
                compilationOptions = compilationOptions.WithCryptoKeyFile(fullPath)
                    .WithStrongNameProvider(new DesktopStrongNameProvider());
                if (delaySign)
                {
                    compilationOptions = compilationOptions.WithDelaySign(true);
                }
            }

            // Build the compilation with all references
            var compilation = CSharpCompilation.Create(dllName)
            .WithOptions(compilationOptions)
            .AddReferences(references)
            .AddSyntaxTrees(tree);
            
            // Add additional references provided by the caller
            if (customReferences != null)
            {
                foreach (var customRef in customReferences)
                {
                    compilation = compilation.AddReferences(MetadataReference.CreateFromFile(customRef));
                }
            }

            EmitResult compilationResult = compilation.Emit(fileName);
            if (compilationResult.Success)
            {
                return;
            }

            Assert.Fail("Unable to compile test assembly: " + dllName + ": " + compilationResult.Diagnostics[0].GetMessage());
        }

        public static void BuildAssemblies(LanguageVersion languageVersion = LanguageVersion.Latest, params string[] names)
        {
            var options = new List<string>();
            foreach (var name in names)
            {
                BuildAssembly(name, null, options, languageVersion: languageVersion);
                options.Add(GetAssemblyPath(name));
            }
        }

        public static Obfuscar.Obfuscator Obfuscate(string xml, bool hideStrings = false)
        {
            Obfuscar.Obfuscator obfuscator = Obfuscar.Obfuscator.CreateFromXml(xml);

            if (hideStrings)
                obfuscator.HideStrings();
            obfuscator.RenameFields();
            obfuscator.RenameParams();
            obfuscator.RenameProperties();
            obfuscator.RenameEvents();
            obfuscator.RenameMethods();
            obfuscator.RenameTypes();
            obfuscator.PostProcessing();
            obfuscator.SaveAssemblies(true);

            return obfuscator;
        }

        public static Obfuscar.Obfuscator BuildAndObfuscate(string name, string suffix, string xml,
            bool hideStrings = false, LanguageVersion languageVersion = LanguageVersion.Latest)
        {
            CleanInput();
            BuildAssembly(name, suffix, languageVersion: languageVersion);
            return Obfuscate(xml, hideStrings);
        }

        public static Obfuscar.Obfuscator BuildAndObfuscate(string[] names, string xml, LanguageVersion languageVersion = LanguageVersion.Latest)
        {
            CleanInput();
            BuildAssemblies(languageVersion, names);
            return Obfuscate(xml);
        }

        private static string GetAssemblyPath(string name)
        {
            return Path.GetFullPath(Path.Combine(InputPath, name + ".dll"));
        }
    }
}
