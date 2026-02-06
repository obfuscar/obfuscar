using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class NestedEnumTypeReferenceTests
    {
        private const string AssemblyName = "AssemblyWithNestedEnumSwitch";
        private const string EntryTypeName = "Api.Entry";

        private static string InvokeExecute(string assemblyPath)
        {
            Assembly assembly = Assembly.LoadFile(assemblyPath);
            Type entryType = assembly.GetType(EntryTypeName, throwOnError: true);
            MethodInfo execute = entryType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(execute);
            return (string)execute.Invoke(null, Array.Empty<object>());
        }

        private static Obfuscator BuildAndObfuscate()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HideStrings' value='false' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='ReuseNames' value='false' />" +
                @"<Module file='$(InPath){2}" + AssemblyName + @".dll' />" +
                @"<ForceNamespace name='Api' />" +
                @"</Obfuscator>",
                TestHelper.InputPath,
                TestHelper.OutputPath,
                Path.DirectorySeparatorChar);

            return TestHelper.BuildAndObfuscate(AssemblyName, string.Empty, xml);
        }

        [Fact]
        public void CheckNestedEnumSwitchBaseline()
        {
            TestHelper.CleanInput();
            TestHelper.BuildAssembly(AssemblyName);
            string inputPath = Path.Combine(TestHelper.InputPath, AssemblyName + ".dll");
            Assert.Equal("Trial", InvokeExecute(inputPath));
        }

        [Fact]
        public void CheckNoDanglingNestedEnumTypeReferencesAfterObfuscation()
        {
            Obfuscator item = BuildAndObfuscate();
            string outputPath = Path.Combine(item.Project.Settings.OutPath, AssemblyName + ".dll");

            using var stream = File.OpenRead(outputPath);
            using var peReader = new PEReader(stream);
            MetadataReader metadataReader = peReader.GetMetadataReader();

            foreach (TypeReferenceHandle handle in metadataReader.TypeReferences)
            {
                var typeRef = metadataReader.GetTypeReference(handle);
                string name = metadataReader.GetString(typeRef.Name);
                Assert.NotEqual("Flag", name);
            }
        }
    }
}
