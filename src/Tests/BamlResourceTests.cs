using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Resources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using Xunit;

namespace ObfuscarTests
{
    public class BamlResourceTests
    {
        [Fact]
        public void EmbeddedBamlIsPreserved()
        {
            // Prepare input and output
            TestHelper.CleanInput();
            var inputPath = TestHelper.InputPath;
            var outputPath = TestHelper.OutputPath;
            Directory.CreateDirectory(inputPath);

            string dllName = "BamlInput.dll";
            string dllPath = Path.Combine(inputPath, dllName);

            // Create a .resources blob containing a .baml entry
            byte[] bamlBytes = new byte[] { 1, 2, 3, 4 };
            byte[] resBytes;
            using (var ms = new MemoryStream())
            {
                using (var rw = new ResourceWriter(ms))
                {
                    rw.AddResource("views/main.baml", new MemoryStream(bamlBytes));
                    rw.Generate();
                }
                resBytes = ms.ToArray();
            }

            // Compile a trivial assembly and embed the .resources
            var code = "public class Dummy { public static void M() {} }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var refs = new[] { MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location) };
            var compilation = CSharpCompilation.Create("BamlInput")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(refs)
                .AddSyntaxTrees(tree);

            var resourceDesc = new ResourceDescription("MyResources.resources", () => new MemoryStream(resBytes), true);
            EmitResult emitResult;
            using (var outStream = new FileStream(dllPath, FileMode.Create, FileAccess.Write))
            {
                emitResult = compilation.Emit(outStream, manifestResources: new[] { resourceDesc });
            }

            Assert.True(emitResult.Success, "Failed to emit test assembly with embedded resources");

            // Run obfuscation
            string sep = Path.DirectorySeparatorChar.ToString();
            string xml = $"<?xml version='1.0'?><Obfuscator><Var name='InPath' value='{inputPath}' /><Var name='OutPath' value='{outputPath}' /><Module file='$(InPath){sep}BamlInput.dll' /></Obfuscator>";
            var obf = TestHelper.Obfuscate(xml);

            // Load obfuscated assembly and inspect manifest resources
            var outAsmPath = Path.Combine(outputPath, dllName);
            Assert.True(File.Exists(outAsmPath), "Obfuscated assembly not produced");

            var asm = Assembly.LoadFile(outAsmPath);
            var names = asm.GetManifestResourceNames();
            // Expect the resources blob to be present
            var resName = names.FirstOrDefault(n => n.EndsWith(".resources", StringComparison.OrdinalIgnoreCase));
            Assert.False(string.IsNullOrEmpty(resName), "No .resources manifest resource found in obfuscated assembly");

            using (var rs = asm.GetManifestResourceStream(resName))
            {
                Assert.NotNull(rs);
                using var rr = new ResourceReader(rs);
                bool found = false;
                foreach (DictionaryEntry entry in rr)
                {
                    if (entry.Key != null && entry.Key.ToString().EndsWith(".baml", StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                Assert.True(found, ".baml entry was not preserved inside the embedded .resources after obfuscation");
            }
        }
    }
}
