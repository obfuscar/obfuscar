using System;
using System.IO;
using System.Reflection;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class CustomAttributeNamedArgumentTests
    {
        private Obfuscator BuildAndObfuscateAssembly()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='HideStrings' value='false' />" +
                @"<Module file='$(InPath){2}AssemblyWithCustomAttributeNamedFieldArgument.dll' />" +
                @"</Obfuscator>",
                TestHelper.InputPath,
                TestHelper.OutputPath,
                Path.DirectorySeparatorChar);

            return TestHelper.BuildAndObfuscate("AssemblyWithCustomAttributeNamedFieldArgument", string.Empty, xml);
        }

        [Fact]
        public void CheckAttributeFieldIsRenamed()
        {
            Obfuscator item = BuildAndObfuscateAssembly();
            ObfuscationMap map = item.Mapping;

            AssemblyDefinition inputAssembly = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, "AssemblyWithCustomAttributeNamedFieldArgument.dll"));
            TypeDefinition attributeType = inputAssembly.MainModule.GetType("TestClasses.TestAttribute");
            FieldDefinition attributeField = attributeType.GetFieldByName("testField");

            ObfuscatedThing field = map.GetField(new FieldKey(attributeField));
            Assert.Equal(ObfuscationStatus.Renamed, field.Status);
            Assert.NotEqual("testField", field.StatusText);
        }

        [Fact]
        public void CheckNamedArgumentRuntimeDecodeStillWorksAfterFieldRename()
        {
            Obfuscator item = BuildAndObfuscateAssembly();
            string outputPath = Path.Combine(item.Project.Settings.OutPath, "AssemblyWithCustomAttributeNamedFieldArgument.dll");

            Assembly assembly = Assembly.LoadFile(outputPath);
            Type entryType = assembly.GetType("TestClasses.AttributeNamedArgumentEntryPoint", throwOnError: true);
            MethodInfo execute = entryType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(execute);

            object result = execute.Invoke(null, Array.Empty<object>());
            Assert.True((bool)result);
        }
    }
}
