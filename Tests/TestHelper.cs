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

        public static void BuildAssembly(string name, string suffix)
        {
            BuildAssembly(name, suffix, null);
        }

        public static void BuildAssembly(string name, string suffix = null, List<string> references = null, string keyFile = null, bool delaySign = false, bool treatWarningsAsErrors = true)
        {
            string dllName = string.IsNullOrEmpty(suffix) ? name : name + suffix;

            string fileName = GetAssemblyPath(dllName);
            if (File.Exists(fileName))
            {
                return;
            }

            string code = File.ReadAllText(Path.Combine(InputPath, name + ".cs"));
            // IMPORTANT: force to C# 10 to avoid extra attributes in assemblies. dotnet/runtime#76032
            var tree = SyntaxFactory.ParseSyntaxTree(code, new CSharpParseOptions(LanguageVersion.CSharp10));

            // Detect the file location for the library that defines the object type
            var systemRefLocation = typeof(object).GetTypeInfo().Assembly.Location;
            // Create a reference to the library
            var systemReference = MetadataReference.CreateFromFile(systemRefLocation);

            var dataContractRefLocation = typeof(DataContractAttribute).GetTypeInfo().Assembly.Location;
            var dataContractReference = MetadataReference.CreateFromFile(dataContractRefLocation);

            var consoleRefLocation = typeof(Console).GetTypeInfo().Assembly.Location;
            var consoleReference = MetadataReference.CreateFromFile(consoleRefLocation);

            var attributeRefLocation = Path.Combine(Path.GetDirectoryName(systemRefLocation), "System.Runtime.dll");           
            var attributeReference = MetadataReference.CreateFromFile(attributeRefLocation);

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

            // A single, immutable invocation to the compiler
            // to produce a library
            var compilation = CSharpCompilation.Create(dllName)
              .WithOptions(compilationOptions)
              .AddReferences(systemReference)
              .AddReferences(dataContractReference)
              .AddReferences(consoleReference)
              .AddReferences(attributeReference)
              .AddSyntaxTrees(tree);
            foreach (var option in references ?? new List<string>())
            {
                compilation = compilation.AddReferences(MetadataReference.CreateFromFile(option));
            }

            EmitResult compilationResult = compilation.Emit(fileName);
            if (compilationResult.Success)
            {
                return;
            }

            Assert.Fail("Unable to compile test assembly: " + dllName + ": " + compilationResult.Diagnostics[0].GetMessage());
        }

        public static void BuildAssemblies(params string[] names)
        {
            var options = new List<string>();
            foreach (var name in names)
            {
                BuildAssembly(name, null, options);
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
            bool hideStrings = false)
        {
            CleanInput();
            BuildAssembly(name, suffix);
            return Obfuscate(xml, hideStrings);
        }

        public static Obfuscar.Obfuscator BuildAndObfuscate(string[] names, string xml)
        {
            CleanInput();
            BuildAssemblies(names);
            return Obfuscate(xml);
        }

        private static string GetAssemblyPath(string name)
        {
            return Path.GetFullPath(Path.Combine(InputPath, name + ".dll"));
        }
    }
}
