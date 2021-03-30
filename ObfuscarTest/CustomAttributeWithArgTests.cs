using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Xunit;

namespace ObfuscarTest
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

            TestHelper.BuildAndObfuscate(name, string.Empty, xml);
            return Path.Combine(outputPath, $"{name}.dll");
        }

        public Tuple<string, string> BuildAndObfuscateTwo()
        {
            string outputPath = TestHelper.OutputPath;
            var name = "AssemblyWithCustomAttrTypeArg";
            var secondModuleName = "AssemblyWithCustomAttrTypeArgSecondModule";
            var xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"<Module file='$(InPath){2}{4}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, name, secondModuleName);

            TestHelper.BuildAndObfuscate(new string[] { name, secondModuleName }, xml);
            return new Tuple<string, string>(Path.Combine(outputPath, $"{name}.dll"), Path.Combine(outputPath, $"{secondModuleName}.dll"));
        }

        [Fact]
        public void Check_for_null()
        {
            var output = BuildAndObfuscate();
            var assmDef = AssemblyDefinition.ReadAssembly(output);

            Assert.Equal(3, assmDef.MainModule.Types.Count);
        }

        [Fact]
        public void CheckTypeAttributeArgs()
        {
            var output = BuildAndObfuscateTwo();
            var assmDef = AssemblyDefinition.ReadAssembly(output.Item2);

            var classBType = assmDef.MainModule.GetType("TestClasses.ClassB");

            var fieldWithAttr = classBType.Fields.First();

            var fieldAttr = fieldWithAttr.CustomAttributes.First();

            CustomAttributeArgument fieldAttrTypeArg = fieldAttr.ConstructorArguments.First();

            var typeArgumentValue = ((Mono.Cecil.CustomAttributeArgument[]) fieldAttrTypeArg.Value)[0].Value;

            Assert.NotEqual("TestClasses.ClassC", typeArgumentValue.ToString()); // should be obfuscated
        }
    }
}
