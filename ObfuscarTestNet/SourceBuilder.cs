using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ObfuscarTestNet
{
    public class SourceBuilder
    {
        readonly List<PortableExecutableReference> _references = new();

        /// <summary>
        /// <see href="https://weblog.west-wind.com/posts/2022/Jun/07/Runtime-CSharp-Code-Compilation-Revisited-for-Roslyn#compiling-code-with-raw-roslyn"/>
        /// </summary>
        public string Build(string fileName, params string[] references)
        {
            AddNetCoreDefaultReferences();
            AddAssemblies(references);

            var source = File.ReadAllText(fileName);

            // Set up compilation Configuration
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var compilation = CSharpCompilation.Create(Path.GetFileName(fileName))
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release))
                .WithReferences(_references)
                .AddSyntaxTrees(tree);

            // Actually compile the code
            var outPath = Path.ChangeExtension(fileName, "dll");
            var compilationResult = compilation.Emit(outPath);


            // Compilation Error handling
            if (!compilationResult.Success)
                throw new Exception(string.Join("\n", compilationResult.Diagnostics));

            return outPath;
        }

        public bool AddAssembly(string assemblyDll)
        {
            if (string.IsNullOrEmpty(assemblyDll)) return false;

            var file = Path.GetFullPath(assemblyDll);

            if (!File.Exists(file))
            {
                // check framework or dedicated runtime app folder
                var path = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
                file = Path.Combine(path, assemblyDll);
                if (!File.Exists(file))
                    return false;
            }

            if (_references.Any(r => r.FilePath == file)) return true;

            try
            {
                var reference = MetadataReference.CreateFromFile(file);
                _references.Add(reference);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private void AddAssemblies(params string[] assemblies)
        {
            foreach (var assembly in assemblies)
                AddAssembly(assembly);
        }

        public bool AddAssembly(Type type)
        {
            try
            {
                if (_references.Any(r => r.FilePath == type.Assembly.Location))
                    return true;

                var systemReference = MetadataReference.CreateFromFile(type.Assembly.Location);
                _references.Add(systemReference);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public void AddNetCoreDefaultReferences()
        {
            var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location) + Path.DirectorySeparatorChar;

            AddAssemblies(
                runtimePath + "System.Private.CoreLib.dll",
                runtimePath + "System.Runtime.dll",
                runtimePath + "System.Console.dll",
                runtimePath + "netstandard.dll",

                runtimePath + "System.Text.RegularExpressions.dll", // IMPORTANT!
                runtimePath + "System.Linq.dll",
                runtimePath + "System.Linq.Expressions.dll", // IMPORTANT!

                runtimePath + "System.IO.dll",
                runtimePath + "System.Net.Primitives.dll",
                runtimePath + "System.Net.Http.dll",
                runtimePath + "System.Private.Uri.dll",
                runtimePath + "System.Reflection.dll",
                runtimePath + "System.ComponentModel.Primitives.dll",
                runtimePath + "System.Globalization.dll",
                runtimePath + "System.Collections.Concurrent.dll",
                runtimePath + "System.Collections.NonGeneric.dll",
                runtimePath + "Microsoft.CSharp.dll"
            );

            // this library and CodeAnalysis libs
            //AddAssembly(GetType()); // Scripting Library
        }
    }
}
