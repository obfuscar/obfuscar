using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class ConstructorParameterObjectAttributeTests
    {
        private const string AssemblyName = "AssemblyWithConstructorParameterObjectAttribute";
        private const string EntryTypeName = "TestClasses.ConstructorParameterObjectAttributeEntryPoint";
        private const string TypeName = "TestClasses.NeedsKeyedDependency";
        private const string AttributeTypeName = "TestClasses.ObjectKeyAttribute";
        private const string ExpectedKey = "my-key";

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
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='false' />" +
                @"<Var name='ReuseNames' value='false' />" +
                @"<Module file='$(InPath){2}" + AssemblyName + @".dll' />" +
                @"</Obfuscator>",
                TestHelper.InputPath,
                TestHelper.OutputPath,
                Path.DirectorySeparatorChar);

            return TestHelper.BuildAndObfuscate(AssemblyName, string.Empty, xml);
        }

        private static CustomAttribute ReadParameterAttribute(AssemblyDefinition assembly)
        {
            TypeDefinition targetType = assembly.MainModule.GetType(TypeName);
            MethodDefinition ctor = targetType.Methods.First(method => method.Name == ".ctor");
            ParameterDefinition parameter = ctor.Parameters[0];
            return parameter.CustomAttributes.First(attr => attr.AttributeTypeName == AttributeTypeName);
        }

        [Fact]
        public void CheckConstructorParameterObjectAttributeBaseline()
        {
            TestHelper.CleanInput();
            TestHelper.BuildAssembly(AssemblyName);
            string inputPath = Path.Combine(TestHelper.InputPath, AssemblyName + ".dll");

            string output = InvokeExecute(inputPath);
            Assert.Equal(ExpectedKey, output);
        }

        [Fact]
        public void CheckConstructorParameterObjectAttributeBaselineMetadata()
        {
            TestHelper.CleanInput();
            TestHelper.BuildAssembly(AssemblyName);
            string inputPath = Path.Combine(TestHelper.InputPath, AssemblyName + ".dll");

            AssemblyDefinition inputAssembly = AssemblyDefinition.ReadAssembly(inputPath);
            CustomAttribute attribute = ReadParameterAttribute(inputAssembly);

            Assert.Single(attribute.ConstructorArguments);
            CustomAttributeArgument argument = attribute.ConstructorArguments[0];
            Assert.Equal("System.String", argument.Type.FullName);
            Assert.Equal("my-key", argument.Value);
        }

        [Fact]
        public void CheckConstructorParameterObjectAttributeValueIsPreservedInMetadata()
        {
            Obfuscator item = BuildAndObfuscate();
            string outputPath = Path.Combine(item.Project.Settings.OutPath, AssemblyName + ".dll");

            AssemblyDefinition outputAssembly = AssemblyDefinition.ReadAssembly(outputPath);
            CustomAttribute attribute = ReadParameterAttribute(outputAssembly);

            Assert.Single(attribute.ConstructorArguments);
            Assert.Equal(ExpectedKey, attribute.ConstructorArguments[0].Value);
        }

        [Fact]
        public void CheckConstructorParameterObjectAttributeAfterObfuscation()
        {
            Obfuscator item = BuildAndObfuscate();
            string outputPath = Path.Combine(item.Project.Settings.OutPath, AssemblyName + ".dll");

            string output = InvokeExecute(outputPath);
            Assert.Equal(ExpectedKey, output);
        }
    }
}
