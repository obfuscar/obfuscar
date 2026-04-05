using System.IO;
using System.Reflection.PortableExecutable;
using Obfuscar.Metadata.Mutable;
using Xunit;

namespace ObfuscarTests
{
    public class EntryPointTests
    {
        [Fact]
        public void CheckExecutableEntryPointIsPreserved()
        {
            string outputPath = TestHelper.OutputPath;
            string sourcePath = Path.Combine(TestHelper.InputPath, "..", "WindowsFormsApplication1.exe");
            string inputPath = Path.Combine(TestHelper.InputPath, "WindowsFormsApplication1.exe");

            if (!File.Exists(inputPath))
            {
                File.Copy(sourcePath, inputPath, true);
            }

            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}WindowsFormsApplication1.exe' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.Obfuscate(xml);

            var outputAssembly = MutableAssemblyDefinition.ReadAssembly(
                Path.Combine(outputPath, "WindowsFormsApplication1.exe"));

            Assert.NotNull(outputAssembly.EntryPoint);

            using var stream = File.OpenRead(Path.Combine(outputPath, "WindowsFormsApplication1.exe"));
            using var peReader = new PEReader(stream);

            Assert.NotNull(peReader.PEHeaders.CorHeader);
            Assert.True(peReader.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress != 0u);
        }
    }
}
