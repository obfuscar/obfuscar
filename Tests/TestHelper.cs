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
using System.IO;
using System.CodeDom.Compiler;
using Xunit;
using System.Text;
using System.Collections.Generic;

namespace ObfuscarTests
{
    static class TestHelper
    {
        public static string InputPath = Path.Combine("..", "..", "Input");

        private static int count;

        public static string OutputPath
        {
            get { return Path.Combine("..", "..", "Output", (count++).ToString()); }
        }

        public static void CleanInput()
        {
            // clean out inputPath
            try
            {
                //foreach (string file in Directory.GetFiles(InputPath, "*.dll"))
                //File.Delete(file);
            }
            catch
            {
            }
        }

        public static void BuildAssembly(string name, string suffix = null)
        {
            BuildAssembly(name, suffix, null);
        }

        public static void BuildAssembly(string name, string suffix = null, string options = null, bool treatWarningsAsErrors = true)
        {
            Microsoft.CSharp.CSharpCodeProvider provider = new Microsoft.CSharp.CSharpCodeProvider();

            CompilerParameters cp = new CompilerParameters();
            cp.GenerateExecutable = false;
            cp.GenerateInMemory = false;
            cp.TreatWarningsAsErrors = treatWarningsAsErrors;

            if (!string.IsNullOrEmpty(options))
                cp.CompilerOptions = options;

            string dllName = string.IsNullOrEmpty(suffix) ? name : name + suffix;

            string fileName = GetAssemblyPath(dllName);
            if (File.Exists(fileName))
            {
                return;
            }

            cp.OutputAssembly = fileName;
            CompilerResults cr = provider.CompileAssemblyFromFile(cp, Path.Combine(InputPath, name + ".cs"));
            if (cr.Errors.HasErrors)
            {
                Assert.True(false, "Unable to compile test assembly:  " + dllName + ":" + cr.Errors[0].ErrorText);
            }
        }

        public static void BuildAssemblies(params string[] names)
        {
            var options = new StringBuilder();
            foreach (var name in names)
            {
                BuildAssembly(name, options: options.ToString());
                options.Append(string.Format(" /reference:{0}", GetAssemblyPath(name)));
            }
        }

        public static Obfuscar.Obfuscator Obfuscate(string xml, bool hideStrings = false)
        {
            Obfuscar.Obfuscator obfuscator = Obfuscar.Obfuscator.CreateFromXml(xml);

            if (hideStrings)
                obfuscator.HideStrings();
            obfuscator.RenameFields(new HashSet<string>());
            obfuscator.RenameParams();
            obfuscator.RenameProperties();
            obfuscator.RenameEvents();
            obfuscator.RenameMethods();
            obfuscator.RenameTypes(new HashSet<string>());
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
            return Path.Combine(InputPath, name + ".dll");
        }
    }
}
