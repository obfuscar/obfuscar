using System.IO;
using System.Xml.Linq;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class MapWriterTests
    {
        private const string AssemblyName = "AssemblyWithRules";

        [Fact]
        public void CheckTextMapWriterOutput()
        {
            var item = CreateObfuscator(AssemblyName);
            var map = item.Mapping;

            var writer = new StringWriter();
            using (var mapWriter = new TextMapWriter(writer))
            {
                mapWriter.WriteMap(map);
            }

            string output = writer.ToString();
            Assert.Contains("Renamed Types:", output);
            Assert.Contains("Skipped Types:", output);
            Assert.Contains("Renamed Resources:", output);
            Assert.Contains("Skipped Resources:", output);
            Assert.Contains(" -> ", output);
        }

        [Fact]
        public void CheckXmlMapWriterOutput()
        {
            var item = CreateObfuscator(AssemblyName);
            var map = item.Mapping;

            var writer = new StringWriter();
            using (var mapWriter = new XmlMapWriter(writer))
            {
                mapWriter.WriteMap(map);
            }

            string output = writer.ToString();
            var doc = XDocument.Parse(output);
            Assert.Equal("mapping", doc.Root.Name);
            Assert.NotNull(doc.Root.Element("renamedTypes"));
            Assert.NotNull(doc.Root.Element("skippedTypes"));
            Assert.NotNull(doc.Root.Element("renamedResources"));
            Assert.NotNull(doc.Root.Element("skippedResources"));
        }

        private static Obfuscator CreateObfuscator(string assemblyName)
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='SuppressIldasm' value='false' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, assemblyName);

            return TestHelper.BuildAndObfuscate(
                assemblyName, string.Empty, xml, useNetFramework: false);
        }
    }
}
