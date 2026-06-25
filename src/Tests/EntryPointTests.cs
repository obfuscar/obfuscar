using System;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using LeXtudio.Metadata.Mutable;
using Xunit;
namespace ObfuscarTests
{
    public class EntryPointTests
    {
        [Fact]
        public void CheckWin32ResourcesArePreserved()
        {
            string outputPath = TestHelper.OutputPath;
            string sourcePath = Path.Combine(TestHelper.InputPath, "..", "WindowsFormsApplication1.exe");
            string inputPath = Path.Combine(TestHelper.InputPath, "WindowsFormsApplication1.exe");

            if (!File.Exists(inputPath))
            {
                File.Copy(sourcePath, inputPath, true);
            }

            // Record the original Win32 resource section size
            int originalResourceSize;
            using (var origStream = File.OpenRead(inputPath))
            using (var origReader = new PEReader(origStream))
            {
                var dir = origReader.PEHeaders.PEHeader?.ResourceTableDirectory;
                originalResourceSize = dir?.Size ?? 0;
            }

            Assert.True(originalResourceSize > 0, "Test fixture must have Win32 resources");

            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}WindowsFormsApplication1.exe' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.Obfuscate(xml);

            using var stream = File.OpenRead(Path.Combine(outputPath, "WindowsFormsApplication1.exe"));
            using var peReader = new PEReader(stream);

            var resourceDir = peReader.PEHeaders.PEHeader?.ResourceTableDirectory;
            Assert.True((resourceDir?.Size ?? 0) > 0, "Win32 resources must be preserved after obfuscation");
        }

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
