using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class InPlaceReplacementTests
    {
        private const string AssemblyName = "AssemblyWithTypes";

        private static string Sha256(string path)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(path);
            var hash = sha.ComputeHash(fs);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        [Fact]
        public void Obfuscation_WithOutPathPointingAtInput_ReplacesOriginalFile()
        {
            TestHelper.CleanInput();
            TestHelper.BuildAssembly(AssemblyName);

            var inputPath = Path.Combine(TestHelper.InputPath, AssemblyName + ".dll");
            Assert.True(File.Exists(inputPath));

            var originalHash = Sha256(inputPath);

            string xml = string.Format(
                "<?xml version='1.0'?><Obfuscator>" +
                "<Var name='InPath' value='{0}' />" +
                "<Var name='OutPath' value='{1}' />" +
                "<Var name='ReuseNames' value='false' />" +
                "<Module file='$(InPath){2}" + AssemblyName + ".dll' />" +
                "</Obfuscator>",
                TestHelper.InputPath,
                TestHelper.InputPath,
                Path.DirectorySeparatorChar);

            // Run obfuscation which should write a temp file next to the original and then replace it.
            TestHelper.Obfuscate(xml);

            Assert.True(File.Exists(inputPath));

            var newHash = Sha256(inputPath);

            Assert.NotEqual(originalHash, newHash);

            // Ensure the resulting assembly is loadable
            var asm = System.Reflection.Assembly.LoadFile(inputPath);
            Assert.NotNull(asm);
            Assert.Equal(AssemblyName, asm.GetName().Name);
        }
    }
}
