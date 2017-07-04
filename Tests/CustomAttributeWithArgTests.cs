using System;
using System.IO;
using Mono.Cecil;
using Xunit;

namespace ObfuscarTests
{
    public class CustomAttributeWithArgTests
    {
        public string BuildAndObfuscate()
        {
            string outputPath = TestHelper.OutputPath;
            var name = "AssemblyWithCustomAttrTypeArg";
            var xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, name);

            TestHelper.BuildAndObfuscate(name, String.Empty, xml);
            return Path.Combine(outputPath, $"{name}.dll");
        }

        [Fact]
        public void Check_for_null()
        {
            var output = BuildAndObfuscate();
            var assmDef = AssemblyDefinition.ReadAssembly(output);

            Assert.Equal(3, assmDef.MainModule.Types.Count);
        }
    }
}
